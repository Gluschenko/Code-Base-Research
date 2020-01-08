﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CodeBase
{
    public partial class ProjectWindow : Window
    {
        public readonly Project[] Projects;

        public ProjectWindow(Project project)
            : this(new Project[] { project }) {}

        public ProjectWindow(Project[] Projects)
        {
            InitializeComponent();

            this.Projects = Projects;
            Title = Title.Replace("{Title}", Projects[0]?.Title ?? "");
            Width *= 2;
            Height *= 1.5f;

            OutputTextBox.Text = "";
            foreach (var proj in Projects) 
            {
                try {
                    OutputTextBox.Text += GetText(proj);
                }
                catch (Exception ex) {
                    OutputTextBox.Text += ex.ToString();
                }
            }

            if (Projects.Length == 1)
            {
                if (Projects[0] == null) Close();
            }
        }

        public string GetText(Project Project)
        {
            if (Project == null) return string.Empty;

            var text = new StringBuilder();

            void push(string line) 
            {
                text.Append(line);
                text.Append(Environment.NewLine);
            }
            //
            if(Project.Title != "")
                push(Project.Title);
            if(Project.Path != "")
                push($"Path: {Project.Path}");
            if(Project.LastEdit != 0)
                push($"Last edit: {UnixTime.ToDateTime(Project.LastEdit)}");

            push("");
            if (Project.Folders.Count > 0)
            {
                push($"Folders: ");

                int i = 0;
                foreach (string folder in Project.Folders)
                {
                    push($"{++i}. {folder}");
                }

                push("");
            }

            var info = Project.Info;

            push($"SLOC/lines: {info.Volume} ({info.Volume.Ratio * 100}%)");
            push("");

            if (info.ExtensionsVolume.Count > 0)
            {
                push($"Extensions ({info.ExtensionsVolume.Count}): ");

                int i = 0;
                var sorted = info.ExtensionsVolume.OrderBy(v => -v.Value.Lines);
                foreach (var pair in sorted)
                {
                    var vol = pair.Value;
                    push($"{++i}. {pair.Key} ".PadRight(40, '.') + $" {vol} ({vol.Ratio * 100}%) [{vol.Files} files]");
                }

                push("");
            }

            if (info.FilesVolume.Count > 0)
            {
                FileTreeNode tree = new FileTreeNode("root");

                push($"Files ({info.FilesVolume.Count}): ");
                foreach (var pair in info.FilesVolume)
                {
                    tree.Add(pair.Key, pair.Value);
                }
                push(tree.ToString());
                push("");
            }

            if (info.Errors.Count > 0)
            {
                push($"Errors ({info.Errors.Count}): ");
                int i = 0;
                foreach (var error in info.Errors)
                {
                    push($"{++i}. {error}");
                }
                push("");
            }

            push("".PadRight(50, '_'));
            push("");
            //
            return text.ToString();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void SourceFilesItem_Selected(object sender, RoutedEventArgs e)
        {
            if (Projects == null) return;

            SourceFilesList.Items.Clear();

            foreach (var proj in Projects)
            {
                if (Path.IsPathRooted(proj.Path))
                {
                    Clipboard.SetText(string.Join("\n", Directory.GetFiles(proj.Path, "*", SearchOption.AllDirectories)).Replace('\\', '/'));

                    var files = new List<FilesListItem>();
                    GetFiles(files, proj.Path);

                    var projectString = $"Project: {proj.Path} ({files.Count} files)";
                    SourceFilesList.Items.Add(new FilesListItem(projectString, FilesListItem.Default));

                    foreach (var file in files)
                    {
                        file.Text = file.Text.Substring(proj.Path.Length);
                        SourceFilesList.Items.Add(file);
                    }
                }
            }
        }

        public void GetFiles(List<FilesListItem> list, string dir, GitIgnoreReader gitIgnore = null) 
        {
            var subs = Directory.GetDirectories(dir).Select(s => s.Replace('\\', '/'));
            var files = Directory.GetFiles(dir).Select(s => s.Replace('\\', '/'));

            if (gitIgnore == null)
            {
                gitIgnore = GitIgnoreReader.Load(dir, true);
            }

            foreach (var file in files) 
            {
                list.Add(new FilesListItem(file, gitIgnore.IsMatch(file) ? FilesListItem.Green : FilesListItem.Red));
            }

            foreach (var sub in subs)
            {
                GetFiles(list, sub, gitIgnore);
            }
        }

        //

        private class FileTreeNode
        {
            public string Key { get; set; } = "";
            public CodeVolume Volume
            {
                get
                {
                    if (nodes.Count > 0)
                    {
                        CodeVolume volume = new CodeVolume();
                        foreach (var node in nodes) volume += node.Value.Volume;
                        return volume;
                    }
                    return _Volume;
                }
            }

            private CodeVolume _Volume;
            readonly Dictionary<string, FileTreeNode> nodes = new Dictionary<string, FileTreeNode>();

            public FileTreeNode(string key)
            {
                Key = key;
                _Volume = new CodeVolume();
            }

            public void Add(string path, CodeVolume volume)
            {
                Add(path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries), volume);
            }

            public void Add(string[] path, CodeVolume volume)
            {
                if (path.Length > 0)
                {
                    string key = path[0];
                    path = path.Where((val, i) => i != 0).ToArray();

                    if (!nodes.ContainsKey(key))
                    {
                        nodes.Add(key, new FileTreeNode(key));
                    }
                    nodes[key].Add(path, volume);
                }
                else
                {
                    _Volume = volume;
                }
            }

            public string ToString(int span)
            {
                string output = "";

                string _key = (nodes.Count > 0 ? "\\" : "") + Key + " ";
                output += $"{_key.PadLeft(span + _key.Length).PadRight(40, '.')} {Volume} ({Volume.Ratio * 100}%)" + Environment.NewLine;

                if (nodes.Count > 0)
                {
                    span += 2;

                    foreach (var node in nodes)
                    {
                        output += node.Value.ToString(span);
                    }
                }

                return output;
            }

            public override string ToString() => ToString(0);
        }
    }

    public class FilesListItem
    {
        public static Brush 
            Default = GetBrush("#222"),
            Green = GetBrush("#0F0"),
            Red = GetBrush("#F00");

        private static Brush GetBrush(string rgb) => 
            new SolidColorBrush((Color) ColorConverter.ConvertFromString(rgb));

        //

        public string Text { get; set; }
        public Brush Color { get; set; }

        public FilesListItem(string text, Brush color)
        {
            Text = text;
            Color = color;
        }

        public FilesListItem() : this("", Green) { }
    }
}
