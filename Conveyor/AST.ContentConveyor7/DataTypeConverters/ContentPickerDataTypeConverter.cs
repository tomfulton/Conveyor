﻿namespace AST.ContentConveyor7.DataTypeConverters
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    using AST.ContentConveyor7;
    using AST.ContentConveyor7.Enums;

    using Umbraco.Core.Models;

    public class ContentPickerDataTypeConverter : BaseContentManagement, IDataTypeConverter
    {
        public void Export(string propertyValue, XElement propertyTag, Dictionary<int, ObjectTypes> dependantNodes)
        {
            if (propertyValue != null && !string.IsNullOrWhiteSpace(propertyValue.ToString()))
            {
                var id = int.Parse(propertyValue.ToString());

                var content = Services.ContentService.GetById(id);
                propertyTag.Value = content.Key.ToString();

                if (!dependantNodes.ContainsKey(content.Id))
                {
                    dependantNodes.Add(content.Id, ObjectTypes.Document);
                }
            }
        }

        public string Import(XElement propertyTag)
        {
            var result = string.Empty;

            if (!string.IsNullOrWhiteSpace(propertyTag.Value))
            {
                var guid = new Guid(propertyTag.Value);
                var content = Services.ContentService.GetById(guid);
                if (content != null)
                {
                    var id = content.Id;
                    result = id.ToString();
                }
            }

            return result;
        }
    }
}