#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion


#pragma warning disable 108

namespace MediaPortal.Configuration.Sections
{
  public class PictureExtensions : BaseFileExtensions
  {
    public PictureExtensions()
      : this("Picture Extensions") {}

    public PictureExtensions(string name)
      : base(name) {}

    protected override string SettingsSection
    {
      get { return "pictures"; }
    }

    protected override string DefaultExtensions
    {
      get { return Util.Utils.PictureExtensionsDefault; }
    }
  }
}