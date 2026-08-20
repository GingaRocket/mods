using System;
using System.Collections.Generic;
using System.Drawing;
using GtaCollectiblesMap.Core.Config;
using GtaCollectiblesMap.Core.Model;
using GTA;
using GTA.Forms;

namespace GtaCollectiblesMap.Ui;

/// <summary>Settings panel built from ScriptHookDotNet's in-game forms controls.</summary>
public sealed class ScriptHookSettingsUi : ISettingsUi
{
    private static readonly Color ProblemColour = Color.FromArgb(235, 110, 110);
    private static readonly Color NormalColour = Color.White;

    private SettingsForm? _form;
    private bool _open;

    public bool IsOpen => _open;

    public void Toggle()
    {
        if (_open)
        {
            Close();
            return;
        }

        if (_form is null)
        {
            _form = new SettingsForm();
            _form.Closed += (_, _) => _open = false;
        }

        _open = true;
        _form.Show();
    }

    public void Close()
    {
        if (!_open)
        {
            return;
        }

        _open = false;
        _form?.Close();
    }

    public void Render(SettingsViewModel model)
    {
        if (_open)
        {
            _form?.Synchronise(model);
        }
    }

    private sealed class SettingsForm : Form
    {
        private readonly Checkbox _showBirds;
        private readonly Checkbox _showJumps;
        private readonly Checkbox _showAll;
        private readonly Checkbox _showAllOnRadar;
        private readonly Listbox _birdColour;
        private readonly Listbox _jumpColour;
        private readonly ValueRow _collectedOpacity;
        private readonly ValueRow _collectedSize;
        private readonly ValueRow _markerSize;
        private readonly ValueRow _radarRadius;
        private readonly ValueRow _refreshInterval;
        private readonly GTA.Forms.Label[] _diagnostics;

        private SettingsViewModel? _model;
        private bool _synchronising;

        public SettingsForm()
        {
            Text = "GTA IV Collectibles Map";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(560, 610);

            _showBirds = AddCheckbox("Show flying rats / seagulls", 12, 12,
                value => Change(settings => settings.ShowBirds = value));
            _showJumps = AddCheckbox("Show stunt jumps", 12, 40,
                value => Change(settings => settings.ShowStuntJumps = value));
            _showAll = AddCheckbox("Show already collected items", 12, 68,
                value => Change(settings => settings.ShowAll = value));
            _showAllOnRadar = AddCheckbox("Collected items also appear on radar", 12, 96,
                value => Change(settings => settings.ShowAllOnMinimap = value));

            AddLabel("Bird colour", 12, 132, 220);
            _birdColour = AddColourList(12, 154, value => Change(settings => settings.BirdColour = value));
            AddLabel("Stunt jump colour", 286, 132, 220);
            _jumpColour = AddColourList(286, 154, value => Change(settings => settings.StuntJumpColour = value));

            _collectedOpacity = AddValueRow("Collected opacity", 12, 292,
                () => Change(settings => settings.CollectedAlpha = Clamp(settings.CollectedAlpha - 0.05f, 0.05f, 1f)),
                () => Change(settings => settings.CollectedAlpha = Clamp(settings.CollectedAlpha + 0.05f, 0.05f, 1f)));
            _collectedSize = AddValueRow("Collected size", 12, 326,
                () => Change(settings => settings.CollectedScale = Clamp(settings.CollectedScale - 0.05f, 0.2f, 1f)),
                () => Change(settings => settings.CollectedScale = Clamp(settings.CollectedScale + 0.05f, 0.2f, 1f)));
            _markerSize = AddValueRow("Marker size", 12, 360,
                () => Change(settings => settings.MarkerScale = Clamp(settings.MarkerScale - 0.1f, 0.2f, 2f)),
                () => Change(settings => settings.MarkerScale = Clamp(settings.MarkerScale + 0.1f, 0.2f, 2f)));
            _radarRadius = AddValueRow("Radar radius", 286, 292,
                () => Change(settings => settings.RadarRadius = Clamp(settings.RadarRadius - 25f, 50f, 1000f)),
                () => Change(settings => settings.RadarRadius = Clamp(settings.RadarRadius + 25f, 50f, 1000f)));
            _refreshInterval = AddValueRow("Refresh interval", 286, 326,
                () => Change(settings => settings.RefreshIntervalMs = Clamp(settings.RefreshIntervalMs - 250, 250, 10000)),
                () => Change(settings => settings.RefreshIntervalMs = Clamp(settings.RefreshIntervalMs + 250, 250, 10000)));

            AddLabel("Status", 12, 410, 520);
            _diagnostics = new GTA.Forms.Label[8];
            for (int i = 0; i < _diagnostics.Length; i++)
            {
                _diagnostics[i] = AddLabel(string.Empty, 12, 434 + (i * 20), 520);
            }
        }

        public void Synchronise(SettingsViewModel model)
        {
            _model = model;
            ModSettings settings = model.Settings;

            _synchronising = true;
            try
            {
                _showBirds.Checked = settings.ShowBirds;
                _showJumps.Checked = settings.ShowStuntJumps;
                _showAll.Checked = settings.ShowAll;
                _showAllOnRadar.Checked = settings.ShowAllOnMinimap;
                _birdColour.SelectedValue = settings.BirdColour;
                _jumpColour.SelectedValue = settings.StuntJumpColour;
            }
            finally
            {
                _synchronising = false;
            }

            _collectedOpacity.Value.Text = settings.CollectedAlpha.ToString("0.00");
            _collectedSize.Value.Text = settings.CollectedScale.ToString("0.00");
            _markerSize.Value.Text = settings.MarkerScale.ToString("0.0");
            _radarRadius.Value.Text = $"{settings.RadarRadius:0} m";
            _refreshInterval.Value.Text = $"{settings.RefreshIntervalMs} ms";

            for (int i = 0; i < _diagnostics.Length; i++)
            {
                if (i < model.Diagnostics.Count)
                {
                    DiagnosticLine diagnostic = model.Diagnostics[i];
                    _diagnostics[i].Text = $"{diagnostic.Name}: {diagnostic.Text}";
                    _diagnostics[i].ForeColor = diagnostic.IsProblem ? ProblemColour : NormalColour;
                }
                else
                {
                    _diagnostics[i].Text = string.Empty;
                }
            }
        }

        private Checkbox AddCheckbox(string text, int x, int y, Action<bool> changed)
        {
            Checkbox control = new()
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(520, 24),
            };
            control.CheckedChanged += (_, _) =>
            {
                if (!_synchronising)
                {
                    changed(control.Checked);
                }
            };
            Controls.Add(control);
            return control;
        }

        private Listbox AddColourList(int x, int y, Action<BlipColour> changed)
        {
            Listbox control = new()
            {
                Location = new Point(x, y),
                Size = new Size(260, 120),
            };

            foreach (BlipColour colour in BlipColours.Selectable())
            {
                control.Items.Add(colour, colour.ToString());
            }

            control.SelectedIndexChanged += (_, _) =>
            {
                if (!_synchronising && control.SelectedValue is BlipColour value)
                {
                    changed(value);
                }
            };
            Controls.Add(control);
            return control;
        }

        private ValueRow AddValueRow(string name, int x, int y, Action decrease, Action increase)
        {
            AddLabel(name, x, y + 8, 140);
            GTA.Forms.Label value = AddLabel(string.Empty, x + 140, y + 8, 70);
            AddButton("-", x + 212, y, decrease);
            AddButton("+", x + 248, y, increase);
            return new ValueRow(value);
        }

        private GTA.Forms.Label AddLabel(string text, int x, int y, int width)
        {
            GTA.Forms.Label label = new()
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 20),
            };
            Controls.Add(label);
            return label;
        }

        private void AddButton(string text, int x, int y, Action clicked)
        {
            Button button = new()
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(32, 30),
            };
            button.Click += (_, _) => clicked();
            Controls.Add(button);
        }

        private void Change(Action<ModSettings> change)
        {
            if (_model is null)
            {
                return;
            }

            change(_model.Settings);
            _model.NotifyChanged();
            Synchronise(_model);
        }

        private static float Clamp(float value, float min, float max) =>
            Math.Max(min, Math.Min(max, value));

        private static int Clamp(int value, int min, int max) =>
            Math.Max(min, Math.Min(max, value));

        private readonly record struct ValueRow(GTA.Forms.Label Value);
    }
}
