using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KellermanSoftware.CompareNetObjects;
using MvvmHelpers.Commands;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Database.PointDetailDataModels;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility.ChangesAndValidation;
using PointlessWaymarksCmsWpfControls.Utility.ThreadSwitcher;

namespace PointlessWaymarksCmsWpfControls.PointDetailEditor
{
    public class PointDetailListContext : IHasChanges, IHasValidationIssues, INotifyPropertyChanged,
        ICheckForChangesAndValidation
    {
        private ObservableCollection<string> _additionalPointDetailTypes;
        private List<PointDetail> _dbEntries;
        private Command<IPointDetailEditor> _deleteDetailCommand;
        private List<IPointDetailEditor> _deletedPointDetails;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private ObservableCollection<IPointDetailEditor> _items;
        private Command<string> _loadNewDetailCommand;
        private List<(string typeIdentifierAttribute, Type reflectedType)> _pointDetailTypeList;
        private StatusControlContext _statusContext;

        private PointDetailListContext(StatusControlContext statusContext)
        {
            StatusContext = statusContext ?? new StatusControlContext();

            DeletedPointDetails = new List<IPointDetailEditor>();

            LoadNewDetailCommand = StatusContext.RunNonBlockingTaskCommand<string>(async x => await LoadNewDetail(x));
            DeleteDetailCommand =
                StatusContext.RunNonBlockingTaskCommand<IPointDetailEditor>(async x => await DeleteDetail(x));
        }

        public ObservableCollection<string> AdditionalPointDetailTypes
        {
            get => _additionalPointDetailTypes;
            set
            {
                if (Equals(value, _additionalPointDetailTypes)) return;
                _additionalPointDetailTypes = value;
                OnPropertyChanged();
            }
        }

        public List<PointDetail> DbEntries
        {
            get => _dbEntries;
            set
            {
                if (Equals(value, _dbEntries)) return;
                _dbEntries = value;
                OnPropertyChanged();
            }
        }

        public Command<IPointDetailEditor> DeleteDetailCommand
        {
            get => _deleteDetailCommand;
            set
            {
                if (Equals(value, _deleteDetailCommand)) return;
                _deleteDetailCommand = value;
                OnPropertyChanged();
            }
        }

        public List<IPointDetailEditor> DeletedPointDetails
        {
            get => _deletedPointDetails;
            set
            {
                if (Equals(value, _deletedPointDetails)) return;
                _deletedPointDetails = value;
                OnPropertyChanged();
            }
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                if (value == _hasChanges) return;
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        public bool HasValidationIssues
        {
            get => _hasValidationIssues;
            set
            {
                if (value == _hasValidationIssues) return;
                _hasValidationIssues = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<IPointDetailEditor> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
                OnPropertyChanged();
            }
        }


        public Command<string> LoadNewDetailCommand
        {
            get => _loadNewDetailCommand;
            set
            {
                if (Equals(value, _loadNewDetailCommand)) return;
                _loadNewDetailCommand = value;
                OnPropertyChanged();
            }
        }

        public StatusControlContext StatusContext
        {
            get => _statusContext;
            set
            {
                if (Equals(value, _statusContext)) return;
                _statusContext = value;
                OnPropertyChanged();
            }
        }

        public void CheckForChangesAndValidationIssues()
        {
            var hasChanges = PropertyScanners.ChildPropertiesHaveChanges(this) ||
                             (Items?.Any(x => x.HasChanges) ?? false);

            if (!hasChanges && Items != null && DbEntries != null)
            {
                var originalItems = DbEntries.Select(x => x.ContentId).ToList();
                var listItems = Items.Select(x => x.DbEntry.ContentId).ToList();

                var logic = new CompareLogic {Config = {IgnoreCollectionOrder = true}};
                hasChanges = !logic.Compare(originalItems, listItems).AreEqual;
            }

            HasChanges = hasChanges;

            HasValidationIssues = PropertyScanners.ChildPropertiesHaveValidationIssues(this) ||
                                  (Items?.Any(x => x.HasValidationIssues) ?? false);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static async Task<PointDetailListContext> CreateInstance(StatusControlContext statusContext,
            PointContent dbEntry)
        {
            var newControl = new PointDetailListContext(statusContext);
            await newControl.LoadData(dbEntry, true);
            return newControl;
        }

        public List<PointDetail> CurrentStateToPointDetailsList()
        {
            var returnList = new List<PointDetail>();
            if (Items == null || !Items.Any()) return returnList;
            return Items.Select(x => x.CurrentPointDetail()).ToList();
        }

        private async Task DeleteDetail(IPointDetailEditor pointDetail)
        {
            await ThreadSwitcher.ResumeForegroundAsync();

            pointDetail.PropertyChanged -= MonitorChildChangesAndValidations;

            Items.Remove(pointDetail);

            DeletedPointDetails.Add(pointDetail);

            CheckForChangesAndValidationIssues();
        }

        public async Task<IPointDetailEditor> ListItemEditorFromTypeIdentifier(PointDetail detail)
        {
            return detail.DataType switch
            {
                "Campground" => await CampgroundPointDetailContext.CreateInstance(detail, StatusContext),
                "Feature" => await FeaturePointDetailContext.CreateInstance(detail, StatusContext),
                "Parking" => await ParkingPointDetailContext.CreateInstance(detail, StatusContext),
                "Peak" => await PeakPointDetailContext.CreateInstance(detail, StatusContext),
                "Restroom" => await RestRoomPointDetailContext.CreateInstance(detail, StatusContext),
                "Trail Junction" => await TrailJunctionPointDetailContext.CreateInstance(detail, StatusContext),
                _ => null
            };
        }

        public async Task LoadData(PointContent dbEntry, bool clearState)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var toLoad = new List<PointDetail>();

            if (dbEntry != null && dbEntry.Id > 0)
            {
                var db = await Db.Context();

                toLoad = db.PointDetails.Where(x => x.PointContentId == dbEntry.ContentId).ToList();
            }

            DbEntries = toLoad.OrderBy(x => x.DataType).ToList();

            if (clearState)
            {
                DeletedPointDetails = new List<IPointDetailEditor>();

                var pointDetailTypes = from type in typeof(Db).Assembly.GetTypes()
                    where typeof(IPointDetailData).IsAssignableFrom(type) && !type.IsInterface
                    select type;

                _pointDetailTypeList = new List<(string typeIdentifierAttribute, Type reflectedType)>();

                foreach (var loopTypes in pointDetailTypes)
                {
                    var typeExample = (IPointDetailData) Activator.CreateInstance(loopTypes);

                    if (typeExample == null) continue;

                    _pointDetailTypeList.Add((typeExample.DataTypeIdentifier, loopTypes));
                }

                _pointDetailTypeList = _pointDetailTypeList.OrderBy(x => x.typeIdentifierAttribute).ToList();
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            Items = new ObservableCollection<IPointDetailEditor>();

            await ThreadSwitcher.ResumeBackgroundAsync();

            foreach (var loopLoad in DbEntries)
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                var toAdd = await ListItemEditorFromTypeIdentifier(loopLoad);
                toAdd.PropertyChanged += MonitorChildChangesAndValidations;
                Items.Add(toAdd);

                await ThreadSwitcher.ResumeBackgroundAsync();
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            AdditionalPointDetailTypes =
                new ObservableCollection<string>(_pointDetailTypeList.Select(x => x.typeIdentifierAttribute));

            PropertyScanners.SubscribeToChildHasChangesAndHasValidationIssues(this, CheckForChangesAndValidationIssues);
        }

        private async Task LoadNewDetail(string typeIdentifier)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            if (string.IsNullOrWhiteSpace(typeIdentifier))
            {
                StatusContext.ToastError("Detail Type is blank???");
                return;
            }

            var removedItems = DeletedPointDetails.Where(x => x.DbEntry.DataType == typeIdentifier).ToList();

            if (removedItems.Any())
            {
                await ThreadSwitcher.ResumeForegroundAsync();

                var toAdd = removedItems.First();

                DeletedPointDetails.Remove(toAdd);

                toAdd.PropertyChanged += MonitorChildChangesAndValidations;

                Items.Add(toAdd);

                CheckForChangesAndValidationIssues();

                return;
            }

            var newDetailEntry = _pointDetailTypeList.Where(x => x.typeIdentifierAttribute == typeIdentifier).ToList();

            if (!newDetailEntry.Any())
            {
                StatusContext.ToastError($"No Detail Type Found Matching {typeIdentifier}?");
                return;
            }

            if (newDetailEntry.Count > 1)
            {
                StatusContext.ToastError($"More than one Detail Type Found Matching {typeIdentifier}?");
                return;
            }

            var newPointDetail = new PointDetail {DataType = newDetailEntry.First().typeIdentifierAttribute};

            await ThreadSwitcher.ResumeForegroundAsync();

            var newDetail = await ListItemEditorFromTypeIdentifier(newPointDetail);
            newDetail.PropertyChanged += MonitorChildChangesAndValidations;

            Items.Add(newDetail);

            CheckForChangesAndValidationIssues();
        }

        private void MonitorChildChangesAndValidations(object sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == null) return;
            if (e.PropertyName.Contains("Changes")) CheckForChangesAndValidationIssues();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (!propertyName.Contains("HasChanges") && !propertyName.Contains("Validation"))
                CheckForChangesAndValidationIssues();
        }
    }
}