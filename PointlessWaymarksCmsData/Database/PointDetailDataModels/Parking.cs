﻿using PointlessWaymarksCmsData.Content;

namespace PointlessWaymarksCmsData.Database.PointDetailDataModels
{
    public class Parking : IPointDetailData
    {
        public string DataTypeIdentifier => "Parking";
        public bool? Fee { get; set; }
        public string Notes { get; set; }
        public string NotesContentFormat { get; set; } = ContentFormatDefaults.Content.ToString();

        public (bool isValid, string validationMessage) Validate()
        {
            var formatValidation = CommonContentValidation.ValidateBodyContentFormat(NotesContentFormat);
            if (!formatValidation.isValid) return (false, formatValidation.explanation);

            return (true, string.Empty);
        }
    }
}