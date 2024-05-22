﻿// OnionFruit Copyright DragonFruit Network <inbox@dragonfruit.network>
// Licensed under LGPL-3.0. Refer to the LICENCE file for more info

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using DragonFruit.OnionFruit.Models;
using ReactiveUI;

namespace DragonFruit.OnionFruit.ViewModels
{
    /// <summary>
    /// Represents the content displayed in the main window toolbar/ribbon
    /// </summary>
    /// <param name="ToggleChecked">Whether the connection toggle is switched</param>
    /// <param name="AllowToggling">Whether the toggle can be clicked</param>
    /// <param name="Background">The background colour to use</param>
    /// <param name="Text">The text to display on the left of the toolbar</param>
    public record ToolbarContent(bool ToggleChecked, bool AllowToggling, IImmutableSolidColorBrush Background, string Text);

    public class MainWindowViewModel : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        private readonly TorSession _session;

        private readonly ObservableAsPropertyHelper<ToolbarContent> _ribbonContent;

        public MainWindowViewModel()
        {
            if (!Design.IsDesignMode)
            {
                throw new InvalidOperationException("This constructor should not be called in a non-design context. Use the other constructor instead.");
            }
        }

        public MainWindowViewModel(TorSession session)
        {
            _session = session;

            // configure event-driven observables, ensuring correct disposal of subscriptions
            var sessionState = Observable.FromEventPattern<EventHandler<TorSession.TorSessionState>, TorSession.TorSessionState>(handler => session.SessionStateChanged += handler, handler => session.SessionStateChanged -= handler).StartWith(new EventPattern<TorSession.TorSessionState>(this, session.State)).ObserveOn(RxApp.MainThreadScheduler);
            var connectionProgress = Observable.FromEventPattern<EventHandler<int>, int>(handler => session.BootstrapProgressChanged += handler, handler => session.BootstrapProgressChanged -= handler).StartWith(new EventPattern<int>(this, 0)).ObserveOn(RxApp.MainThreadScheduler);

            sessionState.Subscribe().DisposeWith(_disposables);
            connectionProgress.Subscribe().DisposeWith(_disposables);

            _ribbonContent = sessionState.CombineLatest(connectionProgress)
                .Select(x => GetRibbonContent(x.First.EventArgs, x.Second.EventArgs))
                .ToProperty(this, x => x.RibbonContent, scheduler: RxApp.MainThreadScheduler);

            // in the future, there should be a way to move this elsewhere
            ToggleConnection = ReactiveCommand.CreateFromTask(ToggleSession, this.WhenAnyValue(x => x.RibbonContent).Select(x => x.AllowToggling).ObserveOn(RxApp.MainThreadScheduler));
        }

        /// <summary>
        /// Command to toggle the connection (i.e. the toggle switch)
        /// </summary>
        public ICommand ToggleConnection { get; }

        /// <summary>
        /// Gets the content of the ribbon (toggle state, text, background colour)
        /// </summary>
        public ToolbarContent RibbonContent => _ribbonContent.Value;

        private async Task ToggleSession()
        {
            if (_session.State is TorSession.TorSessionState.Connecting or TorSession.TorSessionState.Disconnecting)
            {
                return;
            }

            // start session if session is disconnected or null
            if (_session.State is TorSession.TorSessionState.Disconnected)
            {
                await _session.StartSession();
            }
            else
            {
                await _session.StopSession();
            }
        }

        private ToolbarContent GetRibbonContent(TorSession.TorSessionState state, int connectionProgress) => state switch
        {
            TorSession.TorSessionState.Disconnected => new ToolbarContent(false, true, new ImmutableSolidColorBrush(Color.FromRgb(244, 67, 54)), "Tor Disconnected"),
            TorSession.TorSessionState.Connected => new ToolbarContent(true, true, Brushes.Green, "Tor Connected"),

            TorSession.TorSessionState.Connecting when connectionProgress == 0 => new ToolbarContent(true, false, Brushes.DarkOrange, "Tor Connecting"),
            TorSession.TorSessionState.Connecting => new ToolbarContent(true, false, Brushes.DarkOrange, $"Tor Connecting ({connectionProgress}%)"),
            TorSession.TorSessionState.ConnectingStalled => new ToolbarContent(true, true, Brushes.SlateGray, "Tor Connecting"),

            TorSession.TorSessionState.Disconnecting => new ToolbarContent(false, false, Brushes.DarkOrange, "Tor Disconnecting"),

            TorSession.TorSessionState.BlockedProcess => new ToolbarContent(false, false, Brushes.Black, "Tor Process blocked from starting"),
            TorSession.TorSessionState.BlockedProxy => new ToolbarContent(false, false, Brushes.Black, "OnionFruit was blocked from changing proxy settings"),

            TorSession.TorSessionState.KillSwitchTriggered => new ToolbarContent(true, true, Brushes.DeepPink, "Tor Process Killed (Killswitch enabled)"),

            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}