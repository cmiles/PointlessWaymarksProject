using System;
using System.Text;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public interface IHasUnsavedChanges
    {
        bool HasChanges();
    }
}