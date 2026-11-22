using Rigshift.Config;
using Rigshift.Settings;
using Rigshift.Ui;

namespace Rigshift.Tests;

public class SettingsFormTests
{
    private const string UserPath = @"C:\Users\someone\AppData\Roaming\Rigshift\rigshift.json";

    [Fact]
    public void Notes_with_no_active_path_is_the_creates_file_sentence()
    {
        var notes = SettingsSource.Notes(null, UserPath, new List<string>());

        Assert.Equal(new[]
        {
            $"No config file yet. Saving creates {UserPath}."
        }, notes);
    }

    [Fact]
    public void Notes_with_a_whitespace_active_path_is_the_creates_file_sentence()
    {
        var notes = SettingsSource.Notes("   ", UserPath, new List<string>());

        Assert.Equal(new[]
        {
            $"No config file yet. Saving creates {UserPath}."
        }, notes);
    }

    [Fact]
    public void Notes_when_the_loaded_file_differs_keeps_the_leaves_unchanged_sentence()
    {
        var active = @"C:\Users\someone\Desktop\rigshift.json";
        var notes = SettingsSource.Notes(active, UserPath, new List<string>());

        Assert.Equal(new[]
        {
            $"Loaded {active}. Saving writes {UserPath} and leaves the loaded file unchanged."
        }, notes);
        Assert.Contains(UserPath, notes[0]);
        Assert.Contains(active, notes[0]);
    }

    [Fact]
    public void Notes_when_the_paths_match_case_insensitively_returns_only_the_warnings()
    {
        var active = @"c:\users\someone\appdata\roaming\rigshift\rigshift.json";
        var notes = SettingsSource.Notes(active, UserPath, new List<string>
        {
            "Loadouts[3] (name 'web'): start references unknown definition 'missing'."
        });

        Assert.Equal(new[]
        {
            "Loadouts[3] (name 'web'): start references unknown definition 'missing'."
        }, notes);
    }

    [Fact]
    public void Notes_appends_the_non_blank_warnings_in_order()
    {
        var active = @"C:\temp\rigshift.json";
        var notes = SettingsSource.Notes(active, UserPath, new List<string>
        {
            "first warning",
            "   ",
            "second warning"
        });

        Assert.Equal(new[]
        {
            $"Loaded {active}. Saving writes {UserPath} and leaves the loaded file unchanged.",
            "first warning",
            "second warning"
        }, notes);
    }

    [Fact]
    public void Saving_a_draft_with_an_invalid_idle_color_stays_open_and_lists_the_error()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            draft.IdleColor = "nope";
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            Assert.False(form.ErrorsVisible);

            form.ClickSave();

            Assert.Equal(DialogResult.None, form.DialogResult);
            Assert.True(form.ErrorsVisible);
            Assert.Contains("Idle color must be #RRGGBB.", form.ErrorItems);
        });
    }

    [Fact]
    public void A_later_valid_save_hides_the_error_list_and_sets_ok()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            draft.IdleColor = "nope";
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            form.ClickSave();
            Assert.True(form.ErrorsVisible);

            form.IdleColorText = "#112233";
            form.ClickSave();

            Assert.Equal(DialogResult.OK, form.DialogResult);
            Assert.False(form.ErrorsVisible);
        });
    }

    [Fact]
    public void Saving_a_valid_draft_sets_ok_and_writes_the_idle_color()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            form.IdleColorText = "#112233";
            form.ClickSave();

            Assert.Equal(DialogResult.OK, form.DialogResult);
            Assert.Equal("#112233", draft.IdleColor);
        });
    }

    [Fact]
    public void Cancelling_a_draft_with_an_invalid_idle_color_sets_cancel()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            draft.IdleColor = "nope";
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            form.ClickCancel();

            Assert.Equal(DialogResult.Cancel, form.DialogResult);
        });
    }

    [Fact]
    public void A_form_with_one_warning_shows_that_warning()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var warning = "Loadouts[0] (name 'chat'): color must be #RRGGBB.";
            var form = new SettingsForm(draft, new List<string> { warning }, UserPath);

            Assert.True(form.WarningsVisible);
            Assert.Contains(warning, form.WarningText);
            Assert.Equal(Theme.Caution, form.WarningColor);
            Assert.IsType<ThemedTextView>(form.WarningBox);
        });
    }

    [Fact]
    public void A_form_with_no_warnings_hides_the_warning_box()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            Assert.False(form.WarningsVisible);
        });
    }

    [Fact]
    public void The_save_path_label_reads_saves_to_plus_the_path()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            Assert.Equal($"Saves to {UserPath}", form.SavePathText);
        });
    }

    [Fact]
    public void The_form_shows_the_draft_idle_color_as_text_and_draws_the_field_in_that_color()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            draft.IdleColor = "#8A8A8E";
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            Assert.Equal("#8A8A8E", form.IdleColorText);
            Assert.Equal(Theme.Text, form.IdleColorField.TextColor);
            var swatch = form.IdleColorField.SwatchColor;
            Assert.Equal(0x8A, swatch.R);
            Assert.Equal(0x8A, swatch.G);
            Assert.Equal(0x8E, swatch.B);
        });
    }

    [Fact]
    public void Setting_the_idle_color_hex_text_directly_writes_the_draft()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            form.IdleColorField.HexText = "#445566";

            Assert.Equal("#445566", draft.IdleColor);
        });
    }

    [Fact]
    public void Picking_a_color_writes_the_draft_as_uppercase_hex()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);
            form.IdleColorField.PickColor = _ => Color.FromArgb(0xAA, 0xBB, 0xCC);

            form.IdleColorField.Pick();

            Assert.Equal("#AABBCC", draft.IdleColor);
        });
    }

    [Fact]
    public void A_new_form_has_a_fixed_border_and_no_maximize_box()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            Assert.Equal(FormBorderStyle.FixedSingle, form.FormBorderStyle);
            Assert.False(form.MaximizeBox);
        });
    }

    [Fact]
    public void A_new_form_locks_its_size_minimum_size_and_maximum_size_to_780_by_680()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            Assert.Equal(new Size(780, 680), form.Size);
            Assert.Equal(new Size(780, 680), form.MinimumSize);
            Assert.Equal(new Size(780, 680), form.MaximumSize);
        });
    }

    [Fact]
    public void Asking_for_a_larger_size_leaves_the_form_at_780_by_680()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            form.Size = new Size(1200, 900);

            Assert.Equal(new Size(780, 680), form.Size);
        });
    }

    [Fact]
    public void A_new_form_is_not_the_generic_winforms_icon()
    {
        StaThread.Run(() =>
        {
            using var generic = new Form();
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            Assert.NotSame(generic.Icon, form.Icon);
        });
    }

    [Fact]
    public void A_new_form_icon_draws_the_same_pixels_as_the_accent_tinted_asset_icon()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);
            using var asset = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "rigshift.ico"));
            using var tinted = IconTinter.Tint(asset, SettingsForm.IconColor);

            using var formIcon = form.Icon!.ToBitmap();
            using var tintedIcon = tinted.ToBitmap();

            Assert.Equal(tintedIcon.Width, formIcon.Width);
            Assert.Equal(tintedIcon.Height, formIcon.Height);
            for (var x = 0; x < formIcon.Width; x++)
            {
                for (var y = 0; y < formIcon.Height; y++)
                {
                    Assert.Equal(tintedIcon.GetPixel(x, y), formIcon.GetPixel(x, y));
                }
            }
        });
    }

    [Fact]
    public void Every_fully_opaque_pixel_of_a_new_form_icon_is_the_accent()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            Assert.Equal(Color.FromArgb(0x35, 0xC9, 0xE8), SettingsForm.IconColor);

            using var icon = form.Icon!.ToBitmap();
            var opaqueCount = 0;
            for (var x = 0; x < icon.Width; x++)
            {
                for (var y = 0; y < icon.Height; y++)
                {
                    var pixel = icon.GetPixel(x, y);
                    if (pixel.A != 255)
                        continue;   // anti-aliased edge pixel; the tinted-asset fact covers it
                    opaqueCount++;
                    Assert.Equal(0x35, pixel.R);
                    Assert.Equal(0xC9, pixel.G);
                    Assert.Equal(0xE8, pixel.B);
                }
            }

            Assert.True(opaqueCount > 0);
        });
    }

    [Fact]
    public void Disposing_the_form_disposes_the_icon_it_assigned()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);
            var icon = form.Icon!;

            form.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                _ = icon.Handle;
            });
        });
    }

    [Fact]
    public void The_product_version_is_0_dot_1_dot_0_with_no_build_metadata()
    {
        Assert.Equal("0.1.0", SettingsForm.ProductVersion);
        Assert.DoesNotContain('+', SettingsForm.ProductVersion);
    }

    [Fact]
    public void The_window_title_is_rigshift_the_version_and_settings()
    {
        Assert.Equal("Rigshift 0.1.0 Settings", SettingsForm.WindowTitle);
    }

    [Fact]
    public void A_new_form_title_matches_the_window_title()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            Assert.Equal(SettingsForm.WindowTitle, form.Text);
        });
    }

    [Fact]
    public void The_window_uses_the_dark_theme_caption_and_no_minimize_button()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            Assert.Equal(Theme.Bg, form.FormCaptionBackColor);
            Assert.Equal(Theme.Text, form.FormCaptionTextColor);
            Assert.Equal(Theme.Border, form.FormBorderColor);
            Assert.False(form.MinimizeBox);
            Assert.False(form.MaximizeBox);
        });
    }

    [Fact]
    public void Save_is_the_primary_button_and_Cancel_is_outlined()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            var buttons = Descendants(form).OfType<ThemedButton>().ToList();
            var save = buttons.Single(b => b.Text == "Save");
            var cancel = buttons.Single(b => b.Text == "Cancel");
            Assert.True(save.Primary);
            Assert.False(cancel.Primary);
        });
    }

    [Fact]
    public void The_error_list_is_a_themed_list_box()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            Assert.NotNull(Descendants(form).OfType<ThemedListBox>().FirstOrDefault());
        });
    }

    [Fact]
    public void Opening_the_window_selects_the_first_loadout_and_definition()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);

            Assert.Equal(0, form.LoadoutEditor.SelectedIndex);
            Assert.Equal(0, form.DefinitionEditor.SelectedIndex);
            Assert.False(form.LoadoutEditor.HintVisible, "The first loadout should be selected, not the empty hint.");
        });
    }

    private static IEnumerable<Control> Descendants(Control control)
    {
        foreach (Control child in control.Controls)
        {
            yield return child;
            foreach (var deeper in Descendants(child))
                yield return deeper;
        }
    }

    [Fact]
    public void An_unedited_form_is_not_dirty_and_cancelling_does_not_ask()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);
            var calls = 0;
            form.ConfirmDiscard = () =>
            {
                calls++;
                return true;
            };

            Assert.False(form.IsDirty);

            form.ClickCancel();

            Assert.Equal(DialogResult.Cancel, form.DialogResult);
            Assert.Equal(0, calls);
        });
    }

    [Fact]
    public void Selecting_a_row_is_not_an_edit_and_cancelling_does_not_ask()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);
            var calls = 0;
            form.ConfirmDiscard = () =>
            {
                calls++;
                return true;
            };

            form.LoadoutEditor.Select(1);

            Assert.False(form.IsDirty);

            form.ClickCancel();

            Assert.Equal(DialogResult.Cancel, form.DialogResult);
            Assert.Equal(0, calls);
        });
    }

    [Fact]
    public void Cancelling_a_changed_draft_when_declined_keeps_the_edit_and_stays_none()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);
            var calls = 0;
            form.ConfirmDiscard = () =>
            {
                calls++;
                return false;
            };

            form.IdleColorText = "#123456";
            Assert.True(form.IsDirty);

            form.ClickCancel();

            Assert.Equal(DialogResult.None, form.DialogResult);
            Assert.Equal(1, calls);
            Assert.Equal("#123456", draft.IdleColor);
        });
    }

    [Fact]
    public void Cancelling_a_changed_draft_when_accepted_sets_cancel_and_asks_once()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);
            var calls = 0;
            form.ConfirmDiscard = () =>
            {
                calls++;
                return true;
            };

            form.IdleColorText = "#123456";
            Assert.True(form.IsDirty);

            form.ClickCancel();

            Assert.Equal(DialogResult.Cancel, form.DialogResult);
            Assert.Equal(1, calls);
        });
    }

    [Fact]
    public void A_second_allow_close_after_an_accepted_discard_does_not_ask_again()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);
            var calls = 0;
            form.ConfirmDiscard = () =>
            {
                calls++;
                return true;
            };

            form.IdleColorText = "#123456";

            Assert.True(form.AllowClose());
            Assert.Equal(1, calls);

            Assert.True(form.AllowClose());
            Assert.Equal(1, calls);
        });
    }

    [Fact]
    public void Allowing_close_after_a_valid_save_does_not_ask()
    {
        StaThread.Run(() =>
        {
            var draft = SettingsMapper.FromConfig(ConfigLoader.Default());
            var form = new SettingsForm(draft, new List<string>(), UserPath);
            var calls = 0;
            form.ConfirmDiscard = () =>
            {
                calls++;
                return false;
            };

            form.IdleColorText = "#112233";
            form.ClickSave();
            Assert.Equal(DialogResult.OK, form.DialogResult);

            Assert.True(form.AllowClose());
            Assert.Equal(0, calls);
        });
    }
}
