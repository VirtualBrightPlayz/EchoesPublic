using System;
using Godot;
using GodotSteam;

public partial class WorkshopUploadUI : PanelContainer
{
    public uint appId => Steam.GetAppID();

    [Export]
    public AcceptDialog generalPopup;

    [ExportGroup("Selection List")]
    [Export]
    public Control listFormRoot;
    [Export]
    public Button newItemBtn;
    [Export]
    public Control itemListContainer;

    [ExportGroup("Update Form")]
    [Export]
    public Control updateFormRoot;
    [Export]
    public Label titleLabel;
    [Export]
    public LineEdit titleEdit;
    [Export]
    public TextEdit descEdit;
    [Export]
    public LineEdit contentFolderEdit;
    [Export]
    public LineEdit previewFileEdit;
    [Export]
    public LineEdit commentEdit;
    [Export]
    public Button updateBtn;
    [Export]
    public FileDialog folderSelectDialog;
    [Export]
    public FileDialog previewSelectDialog;
    [Export]
    public Button selectFolderBtn;
    [Export]
    public Button selectPreviewBtn;

    public long PublishedFileId { get; private set; } = 0;
    public ulong UpdateHandle { get; private set; } = 0;
    public bool IsNewItem { get; private set; } = false;
    public ulong QueryHandle { get; private set; } = 0;
    public long QueryPage { get; private set; } = 1;

    public override void _Ready()
    {
        base._Ready();
        Steam.ItemCreated += _OnItemCreated;
        Steam.ItemUpdated += _OnItemUpdated;
        Steam.UgcQueryCompleted += _OnUgcQueryComplete;
        newItemBtn.Pressed += CreateItem;
        updateBtn.Pressed += _UpdatePressed;
        folderSelectDialog.DirSelected += _OnDirSelected;
        selectFolderBtn.Pressed += () =>
        {
            folderSelectDialog.CurrentDir = OS.GetUserDataDir();
            folderSelectDialog.PopupFileDialog();
        };
        previewSelectDialog.FileSelected += _OnPreviewSelected;
        selectPreviewBtn.Pressed += () =>
        {
            previewSelectDialog.CurrentDir = OS.GetUserDataDir();
            previewSelectDialog.PopupFileDialog();
        };
        ListItems();
    }

    public void Exit()
    {
        MenuManager.Instance.LoadMenu();
    }

    public void ListItems()
    {
        listFormRoot.Visible = true;
        updateFormRoot.Visible = false;
        newItemBtn.Disabled = false;
        foreach (var item in itemListContainer.GetChildren())
        {
            item.QueueFree();
        }
        QueryHandle = Steam.CreateQueryUserUGCRequest(Steam.GetSteamID(), Steam.UserUgcList.Published, Steam.UgcMatchingUgcType.MatchingUgcTypeAll, Steam.UserUgcListSortOrder.UserugclistsortorderCreationorderdesc, appId, appId, QueryPage);
        Steam.SendQueryUGCRequest(unchecked((long)QueryHandle));
    }

    public void CreateItem()
    {
        Steam.CreateItem(appId, Steam.WorkshopFileType.WorkshopFileTypeCommunity);
        IsNewItem = true;
        newItemBtn.Disabled = true;
    }

    public void ResetForm()
    {
        listFormRoot.Visible = false;
        updateFormRoot.Visible = true;
        UpdateHandle = Steam.StartItemUpdate(appId, PublishedFileId);
        titleLabel.Text = $"Editing {PublishedFileId}";
        titleEdit.Text = string.Empty;
        descEdit.Text = string.Empty;
        updateBtn.Disabled = !IsNewItem;
        if (!IsNewItem)
        {
            QueryHandle = Steam.CreateQueryUGCDetailsRequest([PublishedFileId]);
            if (!Steam.SetReturnMetadata(QueryHandle, true))
            {
                Log.PrintErr("Failed to set return only metadata.");
            }
            Steam.SendQueryUGCRequest(unchecked((long)QueryHandle));
        }
        IsNewItem = false;
    }

    private void _OnItemCreated(long result, long fileId, bool acceptTos)
    {
        if (!IsInstanceValid(this))
            return;
        if (result == (long)ErrorResult.Ok)
        {
            if (acceptTos)
            {
                if (Steam.IsOverlayEnabled())
                {
                    Steam.ActivateGameOverlayToWebPage($"steam://url/CommunityFilePage/{fileId}");
                }
                else
                {
                    OS.ShellOpen($"steam://url/CommunityFilePage/{fileId}");
                }
            }
            PublishedFileId = fileId;
            Steam.SubscribeItem(PublishedFileId);
            ResetForm();
        }
        else
        {
            var err = (ErrorResult)result;
            generalPopup.DialogText = $"Failed to create item: {err}";
            generalPopup.Popup();
        }
    }

    private void _OnItemUpdated(long result, bool acceptTos)
    {
        if (!IsInstanceValid(this))
            return;
        if (result == (long)ErrorResult.Ok)
        {
            if (acceptTos)
            {
                if (Steam.IsOverlayEnabled())
                {
                    Steam.ActivateGameOverlayToWebPage($"steam://url/CommunityFilePage/{PublishedFileId}");
                }
                else
                {
                    OS.ShellOpen($"steam://url/CommunityFilePage/{PublishedFileId}");
                }
            }
            // successfully updated item
            Log.PrintInfo("Successfully updated item.");
            generalPopup.DialogText = "Successfully updated item.";
            generalPopup.Popup();
            ListItems();
        }
        else
        {
            var err = (ErrorResult)result;
            // failed to update item
            Log.PrintErr($"Failed to update item: {err}");
            generalPopup.DialogText = $"Failed to update item: {err}";
            generalPopup.Popup();
        }
    }

    private void _OnUgcQueryComplete(long handle, long result, long resultsReturned, long totalMatching, bool cached)
    {
        if (!IsInstanceValid(this))
            return;
        if (handle == unchecked((long)QueryHandle))
        {
            if (result == (long)ErrorResult.Ok)
            {
                if (updateFormRoot.Visible)
                {
                    var metadata = Steam.GetQueryUGCMetadata(QueryHandle, 0);
                    var details = Steam.GetQueryUGCResult(QueryHandle, 0);
                    titleLabel.Text = $"Editing {details["file_id"]} {details["title"]}";
                    titleEdit.Text = details["title"].ToString();
                    descEdit.Text = details["description"].ToString();
                    // TODO
                    //metadataEdit.Text = metadata;
                    updateBtn.Disabled = false;
                }
                else if (listFormRoot.Visible)
                {
                    for (int i = 0; i < resultsReturned; i++)
                    {
                        var details = Steam.GetQueryUGCResult(QueryHandle, i);
                        if (details["result"].As<ErrorResult>() != ErrorResult.Ok)
                        {
                            Log.PrintErr($"Failed to get UGC details: {details["result"].As<ErrorResult>()}");
                            continue;
                        }
                        var btn = new Button();
                        itemListContainer.AddChild(btn);
                        btn.Pressed += () =>
                        {
                            PublishedFileId = details["file_id"].AsInt64();
                            ResetForm();
                        };
                        btn.Text = details["title"].AsString();
                    }
                }
            }
            else
            {
                var err = (ErrorResult)result;
                Log.PrintErr($"Failed to query item(s): {err}");
            }
        }
        Steam.ReleaseQueryUGCRequest(unchecked((long)QueryHandle));
        QueryHandle = 0;
    }

    private void _OnDirSelected(string dir)
    {
        if (!GameMod.LoadModInfo(dir, out ModInformation info))
        {
            generalPopup.DialogText = $"Failed to load mod \"info.toml\", check the console for more info.";
            generalPopup.Popup();
            return;
        }
        contentFolderEdit.SetDeferred(LineEdit.PropertyName.Text, dir);
        titleEdit.Text = info.Name;
        descEdit.Text = info.Description;
    }

    private void _OnPreviewSelected(string file)
    {
        previewFileEdit.SetDeferred(LineEdit.PropertyName.Text, file);
    }

    private void _UpdatePressed()
    {
        if (string.IsNullOrWhiteSpace(commentEdit.Text) || !DirAccess.DirExistsAbsolute(contentFolderEdit.Text))
        {
            return;
        }
        if (!string.IsNullOrWhiteSpace(previewFileEdit.Text) && FileAccess.FileExists(previewFileEdit.Text))
        {
            Steam.SetItemPreview(UpdateHandle, previewFileEdit.Text);
        }
        Steam.SetItemTitle(UpdateHandle, titleEdit.Text);
        Steam.SetItemDescription(UpdateHandle, descEdit.Text);
        Steam.SetItemContent(UpdateHandle, contentFolderEdit.Text);
        Steam.SubmitItemUpdate(UpdateHandle, commentEdit.Text);
        updateBtn.Disabled = true;
    }
}