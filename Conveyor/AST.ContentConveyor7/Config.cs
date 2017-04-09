namespace AST.ContentConveyor7
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Web.Hosting;
    using System.Xml.Linq;

    public class Config
    {
        #region Fields

        private const string ConfigFile = "/config/" + Constants.ApplicationAlias + ".config";

        private string FileNameWithPath { get; set; }

        #endregion

        #region Methods

        public Dictionary<string, string> GetSpecialDataTypes()
        {
            FileNameWithPath = HostingEnvironment.MapPath(ConfigFile);

            var result = new Dictionary<string, string>();

            if (File.Exists(FileNameWithPath))
            {
                var config = XDocument.Load(FileNameWithPath);

                if (config.Root != null)
                {
                    result = config.Root.Element("SpecialDataTypes").Elements()
                        .ToDictionary(dt => dt.Attribute("alias").Value.ToString(), dt => dt.Attribute("type").Value);
                }
            }

            return result;
        }

        public Dictionary<string, string> GetOtherDataTypes()
        {
            FileNameWithPath = HostingEnvironment.MapPath(ConfigFile);

            var result = new Dictionary<string, string>();

            if (File.Exists(FileNameWithPath))
            {
                var config = XDocument.Load(FileNameWithPath);

                if (config.Root != null)
                {
                    result = config.Root.Element("OtherDataTypes").Elements()
                        .ToDictionary(dt => dt.Attribute("alias").Value, dt => dt.Attribute("name").Value);
                }
            }

            return result;
        }

        #endregion
    }
}