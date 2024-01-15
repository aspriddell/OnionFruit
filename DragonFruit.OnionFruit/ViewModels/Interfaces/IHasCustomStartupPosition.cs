﻿// OnionFruit Copyright DragonFruit Network <inbox@dragonfruit.network>
// Licensed under LGPL-3.0. Refer to the LICENCE file for more info

using Avalonia;
using Avalonia.Platform;

namespace DragonFruit.OnionFruit.ViewModels.Interfaces
{
    public interface IHasCustomStartupPosition
    {
        PixelPoint GetInitialPosition(Screen screen, Size clientSize);
    }
}