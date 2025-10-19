using MGUI.Core.UI;
using Microsoft.Xna.Framework.Content;
using System.Collections.ObjectModel;

namespace MGUI.Samples.Controls
{
    /// <summary>Represents a node in a file system hierarchy for data binding demonstration.</summary>
    public class FileSystemNode
    {
        public string Name { get; set; }
        public ObservableCollection<FileSystemNode> Children { get; set; }

        public FileSystemNode(string name)
        {
            Name = name;
            Children = new ObservableCollection<FileSystemNode>();
        }

        public override string ToString() => Name;
    }

    public class TreeViewSamples : SampleBase
    {
        public TreeViewSamples(ContentManager Content, MGDesktop Desktop)
            : base(Content, Desktop, $"{nameof(Controls)}", "TreeView.xaml")
        {
            // Create a hierarchical data structure for data binding example
            var fileSystem = CreateFileSystemData();

            // Find the TreeView in the loaded XAML and bind the data
            // Note: This would require accessing the loaded window and finding the TreeView control
            // For now, this demonstrates the data structure that would be used
        }

        /// <summary>Creates a sample file system hierarchy for demonstration.</summary>
        private ObservableCollection<FileSystemNode> CreateFileSystemData()
        {
            var root = new ObservableCollection<FileSystemNode>();

            // Create "Documents" folder
            var documents = new FileSystemNode("Documents");
            documents.Children.Add(new FileSystemNode("Resume.pdf"));
            documents.Children.Add(new FileSystemNode("CoverLetter.docx"));
            
            var work = new FileSystemNode("Work");
            work.Children.Add(new FileSystemNode("Project1.xlsx"));
            work.Children.Add(new FileSystemNode("Presentation.pptx"));
            documents.Children.Add(work);
            
            root.Add(documents);

            // Create "Pictures" folder
            var pictures = new FileSystemNode("Pictures");
            pictures.Children.Add(new FileSystemNode("Vacation.jpg"));
            pictures.Children.Add(new FileSystemNode("Family.png"));
            
            var screenshots = new FileSystemNode("Screenshots");
            screenshots.Children.Add(new FileSystemNode("Screenshot1.png"));
            screenshots.Children.Add(new FileSystemNode("Screenshot2.png"));
            pictures.Children.Add(screenshots);
            
            root.Add(pictures);

            // Create "Downloads" folder
            var downloads = new FileSystemNode("Downloads");
            downloads.Children.Add(new FileSystemNode("Setup.exe"));
            downloads.Children.Add(new FileSystemNode("Archive.zip"));
            root.Add(downloads);

            return root;
        }
    }
}
