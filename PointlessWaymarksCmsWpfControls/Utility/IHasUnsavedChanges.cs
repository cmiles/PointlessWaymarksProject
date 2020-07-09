using System;
using System.Text;

namespace PointlessWaymarksCmsWpfControls.Utility
{
    public interface IHasUnsavedChanges
    {
        bool HasChanges();
    }
}