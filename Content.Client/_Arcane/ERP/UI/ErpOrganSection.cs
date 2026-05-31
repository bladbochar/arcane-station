using Content.Shared._Arcane.ERP.Preferences;
using Content.Shared.Humanoid;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client._Arcane.ERP.UI;

public sealed class ErpOrganSection : BoxContainer
{
    private ErpOrganPreferences _prefs = ErpOrganPreferences.Default();
    private bool _settingPreferences;

    private readonly Dictionary<string, OrganControls> _organControls = new();

    public event Action<ErpOrganPreferences>? OnPreferencesChanged;

    public ErpOrganSection()
    {
        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;
        Margin = new Thickness(0, 8, 0, 0);
        IoCManager.InjectDependencies(this);
        Build();
    }

    private void Build()
    {
        AddChild(new Label
        {
            Text = Loc.GetString("erp-organ-section-title"),
            Margin = new Thickness(0, 0, 0, 2),
        });

        foreach (var slotId in ErpOrganSlots.All)
        {
            var container = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                HorizontalExpand = true,
                Margin = new Thickness(0, 2),
            };

            // ── Row 1: label + variant + size ──────────────────────────────
            var row = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                SeparationOverride = 8,
            };

            row.AddChild(new Label
            {
                Text = Loc.GetString($"erp-preferences-tab-organ-{slotId}"),
                MinWidth = 90,
                VAlign = Label.VAlignMode.Center,
            });

            OptionButton? variantBtn = null;
            if (ErpOrganSlots.Variants.TryGetValue(slotId, out var variants))
            {
                variantBtn = new OptionButton { MinWidth = 120 };
                foreach (var v in variants)
                    variantBtn.AddItem(Loc.GetString($"erp-preferences-tab-variant-{v}"), variantBtn.ItemCount);

                variantBtn.OnItemSelected += args =>
                {
                    variantBtn.SelectId(args.Id);
                    NotifyChange(slotId);
                };

                row.AddChild(variantBtn);
            }

            // Size slider
            Slider? sizeSlider = null;
            Label? sizeLabel = null;
            if (ErpOrganSlots.MaxSize.TryGetValue(slotId, out var maxSize))
            {
                sizeLabel = new Label
                {
                    Text = "1",
                    MinWidth = 20,
                    VAlign = Label.VAlignMode.Center,
                };

                sizeSlider = new Slider
                {
                    MinValue = 1,
                    MaxValue = maxSize,
                    Value = 1,
                    HorizontalExpand = true,
                    MinWidth = 80,
                };

                sizeSlider.OnValueChanged += _ =>
                {
                    var snapped = MathF.Round(sizeSlider.Value);
                    if (MathF.Abs(sizeSlider.Value - snapped) > 0.01f)
                        sizeSlider.SetValueWithoutEvent(snapped);
                    sizeLabel.Text = ((int) snapped).ToString();
                    NotifyChange(slotId);
                };

                row.AddChild(new Label
                {
                    Text = Loc.GetString("erp-organ-size-label"),
                    VAlign = Label.VAlignMode.Center,
                });
                row.AddChild(sizeSlider);
                row.AddChild(sizeLabel);
            }

            // "Skin color" checkbox
            var skinCheck = new CheckBox
            {
                Text = Loc.GetString("erp-organ-skin-color-label"),
                Pressed = true,
            };

            row.AddChild(skinCheck);
            container.AddChild(row);

            // ── Row 2: color selector (hidden by default) ──────────────────
            var colorSelector = new ColorSelectorSliders
            {
                SelectorType = ColorSelectorSliders.ColorSelectorType.Hsv,
                Visible = false,
                HorizontalExpand = true,
                Margin = new Thickness(90, 0, 0, 0),
            };

            colorSelector.OnColorChanged += _ => NotifyChange(slotId);

            skinCheck.OnToggled += args =>
            {
                colorSelector.Visible = !args.Pressed;
                NotifyChange(slotId);
            };

            container.AddChild(colorSelector);
            AddChild(container);

            _organControls[slotId] = new OrganControls(container, variantBtn, sizeSlider, skinCheck, colorSelector);
        }
    }

    public void SetSex(Sex sex)
    {
        foreach (var (slotId, ctrl) in _organControls)
        {
            ctrl.Container.Visible = !ErpOrganSlots.SexFilter.TryGetValue(slotId, out var allowed)
                || Array.IndexOf(allowed, sex) >= 0;
        }
    }

    public void SetPreferences(ErpOrganPreferences prefs)
    {
        _settingPreferences = true;
        try
        {
            _prefs = prefs;

            foreach (var slotId in ErpOrganSlots.All)
            {
                if (!_organControls.TryGetValue(slotId, out var ctrl))
                    continue;

                var cfg = prefs.GetOrgan(slotId);

                if (ctrl.Variant != null)
                {
                    var variants = ErpOrganSlots.Variants.TryGetValue(slotId, out var v) ? v : [];
                    var idx = Array.IndexOf(variants, cfg.Variant);
                    ctrl.Variant.SelectId(idx >= 0 ? idx : 0);
                }

                if (ctrl.Size != null)
                    ctrl.Size.Value = Math.Clamp(cfg.Size, ctrl.Size.MinValue, ctrl.Size.MaxValue);

                var hasCustomColor = cfg.Color.HasValue;
                ctrl.SkinCheck.Pressed = !hasCustomColor;
                ctrl.ColorSelector.Visible = hasCustomColor;
                if (hasCustomColor)
                    ctrl.ColorSelector.Color = cfg.Color!.Value;
            }
        }
        finally
        {
            _settingPreferences = false;
        }
    }

    private void NotifyChange(string slotId)
    {
        if (_settingPreferences)
            return;

        if (!_organControls.TryGetValue(slotId, out var ctrl))
            return;

        var variants = ErpOrganSlots.Variants.TryGetValue(slotId, out var v) ? v : [];
        var variantIdx = ctrl.Variant?.SelectedId ?? 0;
        var variant = variantIdx < variants.Length ? variants[variantIdx] : "human";

        var size = (int) MathF.Round(ctrl.Size?.Value ?? 1f);
        var color = ctrl.SkinCheck.Pressed ? (Color?) null : ctrl.ColorSelector.Color;

        _prefs.SetOrgan(slotId, new ErpOrganConfig { Variant = variant, Size = size, Color = color });
        OnPreferencesChanged?.Invoke(_prefs);
    }

    private sealed class OrganControls
    {
        public readonly BoxContainer Container;
        public readonly OptionButton? Variant;
        public readonly Slider? Size;
        public readonly CheckBox SkinCheck;
        public readonly ColorSelectorSliders ColorSelector;

        public OrganControls(BoxContainer container, OptionButton? variant, Slider? size,
            CheckBox skinCheck, ColorSelectorSliders colorSelector)
        {
            Container = container;
            Variant = variant;
            Size = size;
            SkinCheck = skinCheck;
            ColorSelector = colorSelector;
        }
    }
}
