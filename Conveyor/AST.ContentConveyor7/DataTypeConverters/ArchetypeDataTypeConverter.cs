using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Archetype.Models;
using AST.ContentConveyor7.Enums;
using Newtonsoft.Json;
using Umbraco.Core.Models;

namespace AST.ContentConveyor7.DataTypeConverters
{
    public class ArchetypeDataTypeConverter : BaseContentManagement, IDataTypeConverter
    {
        public void Export(string propertyValue, XElement propertyTag, Dictionary<int, ObjectTypes> dependantNodes)
        {
            var archetype = JsonConvert.DeserializeObject<ArchetypeModel>(propertyValue);
            var dataTypeName = propertyTag.Attribute("dataTypeName").Value;
            var dataTypeDefinition = GetDataTypeDefinitionByName(dataTypeName);

            if (archetype == null || !archetype.Any())
            {
                return;
            }

            var archetypeConfig = GetArchetypeConfigFromDataTypeDefintiion(dataTypeDefinition);

            foreach (var fieldset in archetype)
            {
                var fieldsetConfig = archetypeConfig.Fieldsets.FirstOrDefault(f => f.Alias == fieldset.Alias);
                if (fieldsetConfig == null)
                {
                    continue; // Silently fail for old fieldset content that has no dataType?
                }

                var conveyorDtcCustomAttributes = new Dictionary<string, string>();

                foreach (var archetypeProperty in fieldset.GetArchetypeProperties()) // Think we can remove this filter - just imported some bad data
                {
                    var innerPropertyConfig = fieldsetConfig.Properties.FirstOrDefault(p => p.Alias == archetypeProperty.Alias);
                    if (innerPropertyConfig == null)
                    {
                        // a value is stored in the archetype that no longer exists in the prevalues - leave it alone
                        continue;
                    }
                    var innerPropertyDataType = Services.DataTypeService.GetDataTypeDefinitionById(innerPropertyConfig.DataTypeGuid);

                    if (IsSpecialDataType(innerPropertyDataType))
                    {
                        // Create "fake" property to execute DTC Export method in our own context
                        var fakeProperty = CreateFakePropertyTag(archetypeProperty, innerPropertyConfig, innerPropertyDataType);

                        var archetypePropertyValue = archetypeProperty.Value != null 
                            ? archetypeProperty.Value.ToString()
                            : "";

                        DataTypeConverterExport(archetypePropertyValue, fakeProperty, dependantNodes, SpecialDataTypes[innerPropertyDataType.PropertyEditorAlias]);

                        // Some DTC's add custom attributes to the propertyTag - build a list and later add them as Archetype properties for reference during Import
                        foreach (var customAttribute in fakeProperty.GetCustomAttributesAddedByDtc())
                        {
                            conveyorDtcCustomAttributes.Add("conveyor_dtc_" + archetypeProperty.Alias + "___" + customAttribute.Name.ToString(), customAttribute.Value);
                        }

                        // Set transformed value
                        archetypeProperty.Value = fakeProperty.Value;
                    }
                }

                fieldset.AddCustomAttributesToArchetypeProperties(conveyorDtcCustomAttributes);
            }

            propertyTag.Value = JsonConvert.SerializeObject(archetype);  // TODO: Remove "special" properties - see Archetype helper method
        }
        public string Import(XElement propertyTag)
        {
            var result = string.Empty;

            if (!string.IsNullOrWhiteSpace(propertyTag.Value))
            {
                var input = propertyTag.Value;
                var archetypeDataTypeName = propertyTag.Attribute("dataTypeName").Value;
                var archetypeDataTypeDefinition = GetDataTypeDefinitionByName(archetypeDataTypeName);

                result = ProcessArchetypeForImport(input, archetypeDataTypeDefinition);
            }

            return result;
        }

        private string ProcessArchetypeForImport(string archetypeJson, IDataTypeDefinition archetypeDataTypeDefinition)
        {
            var archetype = JsonConvert.DeserializeObject<ArchetypeModel>(archetypeJson);

            if (archetype == null || !archetype.Any() || archetypeDataTypeDefinition == null)
            {
                return archetypeJson;
            }

            var archetypeConfig = GetArchetypeConfigFromDataTypeDefintiion(archetypeDataTypeDefinition);

            foreach (var fieldset in archetype)
            {
                var fieldsetConfig = archetypeConfig.Fieldsets.FirstOrDefault(f => f.Alias == fieldset.Alias);
                var fieldsetCustomAttributes = fieldset.GetConveyorCustomDtcAttributeProperties();
                fieldset.Properties = fieldset.RemoveConveyorCustomDtcAttributeProperties();

                foreach (var archetypeProperty in fieldset.Properties)
                {
                    var innerPropertyConfig = fieldsetConfig.Properties.FirstOrDefault(p => p.Alias == archetypeProperty.Alias);
                    if (innerPropertyConfig == null)
                    {
                        // a value is stored in the archetype that no longer exists in the prevalues - leave it alone
                        continue;
                    }
                    var innerPropertyDataType = Services.DataTypeService.GetDataTypeDefinitionById(innerPropertyConfig.DataTypeGuid);

                    if (IsSpecialDataType(innerPropertyDataType))
                    {
                        var fakePropertyTag = CreateFakePropertyTag(archetypeProperty, innerPropertyConfig, innerPropertyDataType, fieldsetCustomAttributes);

                        var transformedValue = DataTypeConverterImport(fakePropertyTag, innerPropertyDataType.PropertyEditorAlias);

                        archetypeProperty.Value = transformedValue;
                    }
                }
            }

            return JsonConvert.SerializeObject(archetype); // TODO: Remove "special" properties - see Archetype helper method
        }

        private XElement CreateFakePropertyTag(ArchetypePropertyModel archetypeProperty, ArchetypePreValueProperty archetypePropertyConfig, IDataTypeDefinition archetypePropertyDataType, IEnumerable<ArchetypePropertyModel> fieldsetCustomAttributes = null)
        {
            var fakePropertyTag = new XElement(archetypePropertyConfig.Alias);
            fakePropertyTag.SetAttributeValue("propertyEditorAlias", archetypePropertyDataType.PropertyEditorAlias);
            fakePropertyTag.SetAttributeValue("dataTypeName", archetypePropertyDataType.Name);
            fakePropertyTag.Value = archetypeProperty.Value != null ? archetypeProperty.Value.ToString() : "";

            if (fieldsetCustomAttributes != null)
            {
                // Add extra properties which were added by individual DTC's
                var conveyorSystemPropsForThisProperty = fieldsetCustomAttributes.Where(p => p.Alias.StartsWith("conveyor_dtc_" + archetypeProperty.Alias));

                foreach (var prop in conveyorSystemPropsForThisProperty)
                {
                    var attributeName = prop.Alias.Replace("conveyor_dtc_" + archetypeProperty.Alias + "___", "");
                    fakePropertyTag.SetAttributeValue(attributeName, prop.Value);
                }
            }

            return fakePropertyTag;
        }

        private bool IsSpecialDataType(IDataTypeDefinition dataType)
        {
            return IsSpecialDataType(dataType.PropertyEditorAlias);
        }
        private bool IsSpecialDataType(string propertyEditorAlias)
        {
            return SpecialDataTypes.ContainsKey(propertyEditorAlias);
        }

        private void DataTypeConverterExport(string propertyValue, XElement propertyTag, Dictionary<int, ObjectTypes> dependantNodes, string type)
        {
            var t = GetDataTypeConverterInterface(type);

            t.Export(propertyValue.ToString(), propertyTag, dependantNodes);
        }

        private string DataTypeConverterImport(XElement propertyTag, string propertyEditorAlias)
        {
            var type = SpecialDataTypes[propertyEditorAlias];

            var dataTypeConverter = GetDataTypeConverterInterface(type);

            return dataTypeConverter.Import(propertyTag);
        }

        private IDataTypeDefinition GetDataTypeDefinitionByName(string dataTypeName)
        {
            var allDataTypeDefinitions = Services.DataTypeService.GetAllDataTypeDefinitions();
            return allDataTypeDefinitions.FirstOrDefault(x => x.Name == dataTypeName);
        }

        private ArchetypePreValue GetArchetypeConfigFromDataTypeDefintiion(IDataTypeDefinition dataTypeDefinition)
        {
            var dataTypeConfig = Services.DataTypeService.GetPreValuesByDataTypeId(dataTypeDefinition.Id).First();
            return JsonConvert.DeserializeObject<ArchetypePreValue>(dataTypeConfig);
        }
    }
}
