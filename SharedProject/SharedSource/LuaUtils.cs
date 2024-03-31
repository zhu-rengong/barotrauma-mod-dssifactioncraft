using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;
using Barotrauma;
using MoonSharp.Interpreter;
using System;
using Barotrauma.Extensions;

namespace DSSIFactionCraft
{
    internal static class LuaUtils
    {
        public static DynValue SplitToTable(string value, char separator, StringSplitOptions options = StringSplitOptions.None)
        {
            var dynValue = DynValue.NewTable(GameMain.LuaCs.Lua);
            value.Split(separator, options).ForEach(tag => { dynValue.Table.Append(DynValue.NewString(tag)); });
            return dynValue;
        }
    }
}