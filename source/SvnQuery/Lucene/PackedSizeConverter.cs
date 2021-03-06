﻿#region Apache License 2.0

// Copyright 2008-2009 Christian Rodemeyer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Globalization;
using System.Linq;

namespace SvnQuery.Lucene
{
    public static class PackedSizeConverter
    {
        const int Kb = 1024;
        const int Mb = 1024 * 1024;
        const int Gb = 1024 * 1024 * 1024;

        public static string ToSortableString(int size)
        {
            if (size < Kb) return "b" + size.ToString("X3");
            if (size < Mb) return "k" + (size / Kb).ToString("X3");
            if (size < Gb) return "m" + (size / Mb).ToString("X3");

            return "z001";
        }

        public static string ToString(int size)
        {
            if (size < Kb) return size + " bytes";
            if (size < Mb) return (size / Kb) + " kb";
            if (size < Gb) return (size / Mb) + " mb";

            return (size / Gb) + " gb";
        }

        public static int FromSortableString(string size)
        {
            if (string.IsNullOrEmpty(size)) return 0;
            int v = int.Parse(size.Substring(1), NumberStyles.HexNumber);
            switch (size[0])
            {
                case 'b':
                    return v;
                case 'k':
                    return v * Kb + Kb - 1;
                case 'm':
                    return v * Mb + Mb - 1;
                case 'z':
                    return v * Gb + Gb - 1;
            }
            throw new ArgumentException("size is not a packed size");
        }

        public static string FromSortableStringToString(string size)
        {
            return ToString(FromSortableString(size));
        }
    }
}