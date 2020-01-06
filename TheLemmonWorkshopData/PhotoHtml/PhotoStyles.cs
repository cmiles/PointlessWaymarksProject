namespace TheLemmonWorkshopData.PhotoHtml
{
    public static class PhotoStyles
    {
        public static string PhotoBodyStyle()
        {
            return @"
            body {
                font-family: Geneva, ‘Lucida Sans’, ‘Lucida Grande’, ‘Lucida Sans Unicode’, Verdana, sans-serif;
                color: rgb(220, 220, 220);
                margin-top: 0;
                font-size: 16px;
            }";
        }

        public static string PhotoDetailsStyles()
        {
            return @"
            .photo-details-container {
                display: flex;
                justify-content: center;
                flex-wrap: wrap;
                align-items: center;
                margin-top: 1rem;
            }

            .photo-detail {
                margin: 3px;
                box-shadow: 0 1px 3px rgba(0,0,0,0.12), 0 1px 2px rgba(0,0,0,0.24);
                transition: all 0.3s cubic-bezier(.25,.8,.25,1);
                border-radius: 4px;
            }

            .photo-detail-content {
                margin: 6px;
            }";
        }

        public static string SinglePhotoStyles()
        {
            return @"
            .single-photo-container {
                text-align: center;
                display: table;
                margin-top: 1vh;
                margin-left: auto;
                margin-right: auto;
            }

            .single-photo {
                width: auto;
                height: auto;
                max-width: 100%;
                max-height: 98vh;
            }

            .single-photo-caption {
                text-align: center;
                margin-top: 1rem;
                display: table-caption;
                caption-side: bottom;
            }";
        }
    }
}