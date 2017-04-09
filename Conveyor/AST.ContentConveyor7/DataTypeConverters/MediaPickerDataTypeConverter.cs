﻿namespace AST.ContentConveyor7.DataTypeConverters
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    using AST.ContentConveyor7;
    using AST.ContentConveyor7.Enums;
    using AST.ContentConveyor7.Utilities;

    using Umbraco.Core.Models;

    public class MediaPickerDataTypeConverter : BaseContentManagement, IDataTypeConverter
    {
        public void Export(string propertyValue, XElement propertyTag, Dictionary<int, ObjectTypes> dependantNodes)
        {
            if (propertyValue != null && !string.IsNullOrWhiteSpace(propertyValue.ToString()))
            {
                var id = int.Parse(propertyValue.ToString());
                var media = Services.MediaService.GetById(id);

                // TODO for v6
                if (media != null && FileHelpers.FileExists(media.GetValue("umbracoFile").ToString())) 
                {
                    propertyTag.Value = media.Key.ToString();

                    if (!dependantNodes.ContainsKey(media.Id))
                    {
                        dependantNodes.Add(media.Id, ObjectTypes.Media);
                    }
                }
            }
        }

        public string Import(XElement propertyTag)
        {
            var result = string.Empty;

            if (!string.IsNullOrWhiteSpace(propertyTag.Value))
            {
                var guid = new Guid(propertyTag.Value);
                var id = Services.MediaService.GetById(guid).Id;
                result = id.ToString();
            }

            return result;
        }
    }
}