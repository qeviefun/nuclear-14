// #Misfits Change - Enriched job row with whitelist, role time, and slot controls
using System.Globalization;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;

namespace Content.Client._Misfits.Administration.UI;

public sealed class WhitelistJobRow : PanelContainer
{
    public Action<Robust.Shared.Prototypes.ProtoId<JobPrototype>, bool>? OnSetJob;
    public Action<Robust.Shared.Prototypes.ProtoId<JobPrototype>, string>? OnAddRoleTime;
    public Action<Robust.Shared.Prototypes.ProtoId<JobPrototype>, int>? OnAdjustJobSlots;

    public WhitelistJobRow(
        Robust.Shared.Prototypes.ProtoId<JobPrototype> jobId,
        JobPrototype job,
        Content.Shared._Misfits.Administration.WhitelistSearchMode mode,
        bool whitelisted,
        TimeSpan roleTime,
        int? slots,
        bool hasSlotConfiguration,
        bool canManagePlaytime,
        bool canManageSlots,
        bool hasStation)
    {
        Margin = new Thickness(0, 0, 0, 2);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            HorizontalExpand = true,
        };

        var showWhitelistControls = mode == Content.Shared._Misfits.Administration.WhitelistSearchMode.RoleWhitelists;
        var showRoleTimeControls = mode == Content.Shared._Misfits.Administration.WhitelistSearchMode.RoleWhitelists;
        var showSlotControls = mode == Content.Shared._Misfits.Administration.WhitelistSearchMode.JobSlots;

        var whitelistBox = new CheckBox
        {
            Pressed = whitelisted,
            VerticalAlignment = VAlignment.Center,
            MinWidth = 18,
        };

        whitelistBox.OnPressed += _ => OnSetJob?.Invoke(jobId, whitelistBox.Pressed);

        var jobLabel = new Label
        {
            Text = job.LocalizedName,
            MinWidth = 180,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        };

        var roleTimeLabel = new Label
        {
            Text = Loc.GetString("misfits-whitelist-search-role-time", ("time", FormatTime(roleTime))),
            MinWidth = 120,
            VerticalAlignment = VAlignment.Center,
        };

        var addTimeInput = new LineEdit
        {
            PlaceHolder = Loc.GetString("misfits-whitelist-search-add-time-placeholder"),
            MinWidth = 80,
            Editable = canManagePlaytime,
        };

        var addTimeButton = new Button
        {
            Text = Loc.GetString("misfits-whitelist-search-add-time"),
            Disabled = !canManagePlaytime,
            MinWidth = 72,
            StyleClasses = { "ButtonSquare" },
        };

        addTimeButton.OnPressed += _ =>
        {
            var timeString = addTimeInput.Text.Trim();
            if (timeString.Length == 0)
                return;

            OnAddRoleTime?.Invoke(jobId, timeString);
            addTimeInput.Text = string.Empty;
        };

        var slotText = hasStation
            ? FormatSlotText(slots, hasSlotConfiguration)
            : Loc.GetString("misfits-whitelist-search-slot-no-station");

        var slotLabel = new Label
        {
            Text = slotText,
            MinWidth = 100,
            VerticalAlignment = VAlignment.Center,
        };

        var minusButton = new Button
        {
            Text = "-",
            MinWidth = 28,
            Disabled = !canManageSlots || !hasStation || slots == null,
            StyleClasses = { "OpenRight" },
        };

        var plusButton = new Button
        {
            Text = "+",
            MinWidth = 28,
            Disabled = !canManageSlots || !hasStation || slots == null,
            StyleClasses = { "OpenLeft" },
        };

        minusButton.OnPressed += _ => OnAdjustJobSlots?.Invoke(jobId, -1);
        plusButton.OnPressed += _ => OnAdjustJobSlots?.Invoke(jobId, 1);

        if (!job.Whitelisted)
            whitelistBox.Modulate = Color.FromHex("#cccccc");

        if (showWhitelistControls)
            root.AddChild(whitelistBox);

        root.AddChild(jobLabel);

        if (showRoleTimeControls)
        {
            root.AddChild(roleTimeLabel);
            root.AddChild(addTimeInput);
            root.AddChild(addTimeButton);
        }

        if (showSlotControls)
        {
            root.AddChild(slotLabel);
            root.AddChild(minusButton);
            root.AddChild(plusButton);
        }

        AddChild(root);
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours < 1)
            return Math.Round(time.TotalMinutes).ToString("0", CultureInfo.InvariantCulture) + "m";

        if (time.TotalDays < 1)
            return time.TotalHours.ToString("0.#", CultureInfo.InvariantCulture) + "h";

        return time.TotalDays.ToString("0.#", CultureInfo.InvariantCulture) + "d";
    }

    private static string FormatSlotText(int? slots, bool hasSlotConfiguration)
    {
        if (slots == null)
            return Loc.GetString("misfits-whitelist-search-slot-unlimited");

        var value = hasSlotConfiguration ? slots.Value : 0;
        return Loc.GetString("misfits-whitelist-search-slot-count", ("count", value));
    }
}