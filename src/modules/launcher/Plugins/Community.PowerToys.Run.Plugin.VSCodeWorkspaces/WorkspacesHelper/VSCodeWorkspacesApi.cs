// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Community.PowerToys.Run.Plugin.VSCodeWorkspaces.VSCodeHelper;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.VSCodeWorkspaces.WorkspacesHelper
{
    public class VSCodeWorkspacesApi
    {
        public VSCodeWorkspacesApi()
        {
        }

        private VSCodeWorkspace ParseVSCodeUri(string uri, VSCodeInstance vscodeInstance, bool isWorkspaceFile = false)
        {
            if (uri != null && uri is string)
            {
                string unescapeUri = Uri.UnescapeDataString(uri);
                var typeWorkspace = WorkspacesHelper.ParseVSCodeUri.GetWorkspaceEnvironment(unescapeUri);
                if (typeWorkspace.WorkspaceEnvironment.HasValue)
                {
                    var folderName = Path.GetFileName(unescapeUri);

                    // Check we haven't returned '' if we have a path like C:\
                    if (string.IsNullOrEmpty(folderName))
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(unescapeUri);
                        folderName = dirInfo.Name.TrimEnd(':');
                    }

                    return new VSCodeWorkspace()
                    {
                        Path = uri,
                        WorkspaceType = isWorkspaceFile ? WorkspaceType.WorkspaceFile : WorkspaceType.ProjectFolder,
                        RelativePath = typeWorkspace.Path,
                        FolderName = folderName,
                        ExtraInfo = typeWorkspace.MachineName,
                        WorkspaceEnvironment = typeWorkspace.WorkspaceEnvironment.Value,
                        VSCodeInstance = vscodeInstance,
                    };
                }
            }

            return null;
        }

        public List<VSCodeWorkspace> Workspaces
        {
            get
            {
                var results = new List<VSCodeWorkspace>();

                foreach (var vscodeInstance in VSCodeInstances.Instances)
                {
                    // storage.json contains opened Workspaces
                    var vscode_storage = Path.Combine(vscodeInstance.AppData, "storage.json");

                    if (File.Exists(vscode_storage))
                    {
                        var fileContent = File.ReadAllText(vscode_storage);

                        try
                        {
                            VSCodeStorageFile vscodeStorageFile = JsonSerializer.Deserialize<VSCodeStorageFile>(fileContent);

                            if (vscodeStorageFile != null)
                            {
                                // for previous versions of vscode
                                if (vscodeStorageFile.OpenedPathsList.Workspaces3 != null)
                                {
                                    foreach (var workspaceUri in vscodeStorageFile.OpenedPathsList.Workspaces3)
                                    {
                                        var workspace = ParseVSCodeUri(workspaceUri, vscodeInstance);
                                        if (workspace != null)
                                        {
                                            results.Add(workspace);
                                        }
                                    }
                                }

                                // vscode v1.55.0 or later
                                if (vscodeStorageFile.OpenedPathsList.Entries != null)
                                {
                                    foreach (var entry in vscodeStorageFile.OpenedPathsList.Entries)
                                    {
                                        bool isWorkspaceFile = false;
                                        var uri = entry.FolderUri;
                                        if (entry.Workspace != null && entry.Workspace.ConfigPath != null)
                                        {
                                            isWorkspaceFile = true;
                                            uri = entry.Workspace.ConfigPath;
                                        }

                                        var workspace = ParseVSCodeUri(uri, vscodeInstance, isWorkspaceFile);
                                        if (workspace != null)
                                        {
                                            results.Add(workspace);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var message = $"Failed to deserialize ${vscode_storage}";
                            Log.Exception(message, ex, GetType());
                        }
                    }
                }

                return results;
            }
        }
    }
}
