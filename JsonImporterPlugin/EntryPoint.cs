using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Acuerdo.Plugin;
using Dulcet.Twitter;
using Inscribe.Core;
using Inscribe.Storage;
using Microsoft.Win32;

namespace JsonImporterPlugin
{
    [Export(typeof(IPlugin))]
    public class EntryPoint : IPlugin
    {
        public string Name
        {
            get { return "JSON Importer"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }

        public void Loaded()
        {
            KernelService.AddMenu("Import JSON", () =>
            {
                var dialog = new OpenFileDialog();
                dialog.Filter = "JSON Format|*.json|すべてのファイル|*.*";
                dialog.Multiselect = true;
                var result = dialog.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    Task.Factory.StartNew(() =>
                    {
                        dialog.FileNames.AsParallel().ForAll(file =>
                        {
                            var fileName = Path.GetFileName(file);
                            using (var notify = NotifyStorage.NotifyManually(fileName + " を読み込んでいます"))
                            {
                                try
                                {
                                    using (var reader = JsonReaderWriterFactory.CreateJsonReader(new FileStream(file, FileMode.Open, FileAccess.Read), XmlDictionaryReaderQuotas.Max))
                                    {
                                        XElement.Load(reader).Elements()
                                            .Select(TwitterStatus.FromNode)
                                            .ForEach(s => TweetStorage.Register(s));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ExceptionStorage.Register(ex, ExceptionCategory.UserError, fileName + " を読み込めませんでした");
                                }
                            }
                        });
                    });
                }
            });
        }

        public IConfigurator ConfigurationInterface
        {
            get { return null; }
        }
    }
}
