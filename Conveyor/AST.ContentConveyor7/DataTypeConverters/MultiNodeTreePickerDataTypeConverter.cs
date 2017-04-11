﻿namespace AST.ContentConveyor7.DataTypeConverters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using AST.ContentConveyor7;
    using AST.ContentConveyor7.Enums;

    using umbraco;

    using Umbraco.Core;
    using Umbraco.Core.Models;

    public class MultiNodeTreePickerDataTypeConverter : BaseContentManagement, IDataTypeConverter
    {
        public void Export(string propertyValue, XElement propertyTag, Dictionary<int, ObjectTypes> dependantNodes)
        {
            if (propertyValue != null && !string.IsNullOrWhiteSpace(propertyValue.ToString()))
            {
                int[] nodeIds;

                if (XmlHelper.CouldItBeXml(propertyValue.ToString()))
                {
                    nodeIds = uQuery.GetXmlIds(propertyValue.ToString());
                }
                else
                {
                    nodeIds = propertyValue.ToString().Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                }

                if (nodeIds.Length > 0)
                {
                    var nodeType = uQuery.GetUmbracoObjectType(nodeIds[0]).ToString();

                    propertyTag.Add(new XAttribute("objectType", nodeType));

                    var guidList = new List<Guid>();

                    foreach (var id in nodeIds)
                    {
                        if (nodeType == ObjectTypes.Document.ToString())
                        {
                            var node = Services.ContentService.GetById(id);

                            // the node reference may have been deleted
                            if (node == null)
                            {
                                continue;
                            }

                            var guid = node.Key;

                            guidList.Add(guid);
                            if (!dependantNodes.ContainsKey(id))
                            {
                                dependantNodes.Add(id, ObjectTypes.Document);
                            }
                        }
                        else if (nodeType == ObjectTypes.Media.ToString())
                        {
                            var guid = Services.MediaService.GetById(id).Key;
                            guidList.Add(guid);

                            if (!dependantNodes.ContainsKey(id))
                            {
                                dependantNodes.Add(id, ObjectTypes.Media);
                            }
                        }
                    }

                    propertyTag.Value = string.Join(",", guidList);
                }
            }
        }

        public string Import(XElement propertyTag)
        {
            var result = string.Empty;

            if (!string.IsNullOrWhiteSpace(propertyTag.Value))
            {
                var listOfGuid = propertyTag.Value.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                var objectType = propertyTag.Attribute("objectType").Value;

                var listOfIds = new List<int>();

                foreach (var guid in listOfGuid)
                {
                    if (objectType == ObjectTypes.Document.ToString())
                    {
                        var content = Services.ContentService.GetById(new Guid(guid));

                        if (content != null)
                        {
                            listOfIds.Add(content.Id);
                        }
                    }
                    else if (objectType == ObjectTypes.Media.ToString())
                    {
                        var media = Services.MediaService.GetById(new Guid(guid));
                        
                        if (media != null)
                        {
                            listOfIds.Add(media.Id);
                        }
                    }
                }

                if (listOfIds.Any())
                {
                    result = string.Join(",", listOfIds);
                }
            }

            return result;
        }
    }
}