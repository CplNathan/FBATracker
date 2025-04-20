// <copyright file="RegexConstants.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

namespace FBATrackerReact.Server.Constants;

using System.Text.RegularExpressions;

public partial class RegexConstants
{
    [GeneratedRegex("^(B[\\dA-Z]{9}|\\d{9}(X|\\d))$")]
    public static partial Regex AsinPattern();

    public static partial class SellerAmp
    {
        [GeneratedRegex("var u = ({.*});")]
        public static partial Regex ScriptExtractRegex();
    }
}
