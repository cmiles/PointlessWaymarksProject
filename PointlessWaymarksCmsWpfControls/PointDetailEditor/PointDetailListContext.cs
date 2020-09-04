using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PointlessWaymarksCmsData.Database;
using PointlessWaymarksCmsData.Database.Models;
using PointlessWaymarksCmsData.Database.PointDetailModels;
using PointlessWaymarksCmsWpfControls.Status;
using PointlessWaymarksCmsWpfControls.Utility;

namespace PointlessWaymarksCmsWpfControls.PointDetailEditor
{
    public class PointDetailListContext : IHasChanges, IHasValidationIssues, INotifyPropertyChanged
    {
        private ObservableCollection<string> _additionalPointDetailTypes;
        private bool _hasChanges;
        private bool _hasValidationIssues;
        private ObservableCollection<PointDetail> _items;
        private List<(string typeIdentifierAttribute, Type reflectedType)> _pointDetailTypeList;
        private StatusControlContext _statusContext;

        public PointDetailListContext(StatusControlContext statusContext, PointContent dbEntry)
        {
            StatusContext = statusContext ?? new StatusControlContext();
            StatusContext.RunFireAndForgetTaskWithUiToastErrorReturn(async () => await LoadData(dbEntry));
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

        public ObservableCollection<PointDetail> Items
        {
            get => _items;
            set
            {
                if (Equals(value, _items)) return;
                _items = value;
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

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task LoadData(PointContent dbEntry)
        {
            await ThreadSwitcher.ResumeBackgroundAsync();

            var toLoad = new List<PointDetail>();

            if (dbEntry != null && dbEntry.Id > 0)
            {
                var db = await Db.Context();
                var pointDetailsIds = db.PointContentPointDetailLinks.Where(x => x.PointContentId == dbEntry.ContentId)
                    .Select(x => x.PointDetailContentId).ToList();

                toLoad = db.PointDetails.Where(x => pointDetailsIds.Contains(x.ContentId)).ToList();
            }

            var pointDetailTypes = from type in typeof(Db).Assembly.GetTypes()
                where typeof(IPointDetail).IsAssignableFrom(type)
                select type;

            _pointDetailTypeList = new List<(string typeIdentifierAttribute, Type reflectedType)>();

            foreach (var loopTypes in pointDetailTypes)
            {
                var typeExample = (IPointDetail) Activator.CreateInstance(loopTypes);

                if (typeExample == null) continue;

                _pointDetailTypeList.Add((typeExample.DataTypeIdentifier, loopTypes));
            }

            _pointDetailTypeList = _pointDetailTypeList.OrderBy(x => x.typeIdentifierAttribute).ToList();

            await ThreadSwitcher.ResumeForegroundAsync();

            Items = new ObservableCollection<PointDetail>();

            await ThreadSwitcher.ResumeBackgroundAsync();

            foreach (var loopLoad in toLoad.OrderBy(x => x.DataType).ToList())
            {
                var toRemoveFromTypeList = _pointDetailTypeList
                    .Where(x => x.typeIdentifierAttribute == loopLoad.DataType).ToList();
                _pointDetailTypeList = _pointDetailTypeList.Except(toRemoveFromTypeList).ToList();

                await ThreadSwitcher.ResumeForegroundAsync();

                Items.Add(loopLoad);

                await ThreadSwitcher.ResumeBackgroundAsync();
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            AdditionalPointDetailTypes =
                new ObservableCollection<string>(_pointDetailTypeList.Select(x => x.typeIdentifierAttribute));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}