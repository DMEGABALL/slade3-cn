// -----------------------------------------------------------------------------
// SLADE - It's a Doom Editor
// Copyright(C) 2008 - 2026 Simon Judd
//
// Email:       sirjuddington@gmail.com
// Web:         http://slade.mancubus.net
// Filename:    Translation.h
// Description: Runtime UI string translation (used by the Chinese localisation)
//
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation; either version 2 of the License, or (at your option)
// any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
// more details.
//
// You should have received a copy of the GNU General Public License along with
// this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA  02110 - 1301, USA.
// -----------------------------------------------------------------------------
#pragma once

// -----------------------------------------------------------------------------
//
// Translation
//
// Loads an external, human-editable translation table (by default
// 'lang/zh_CN.txt' next to the SLADE executable) and looks up UI strings in it.
//
// The table is a plain UTF-8 text file, one entry per line:
//
//   <english source text><TAB><translated text>
//
// Lines starting with '#' are comments. '\\n', '\\t' and '\\\\' are unescaped,
// so multi-line strings can be translated as well.
//
// Two directives are understood at the top of the file:
//
//   @mode both     -> show "翻译 (English)"          (default)
//   @mode zh       -> show only the translation
//   @mode en       -> show the original English only
//
// A single entry may override the mode by appending a third tab-separated
// field with one of: both, zh, en.
//
// -----------------------------------------------------------------------------
namespace slade::i18n
{
enum class DisplayMode
{
	Chinese,   // Only the translated text
	Bilingual, // "翻译 (English)"
	English    // Only the original English text
};

// Loads the translation table for [lang] (eg. "zh_CN"). Returns true if a table
// was found and loaded successfully.
bool init(string_view lang = "zh_CN");

// True if a translation table has been loaded
bool active();

// The display mode currently in use
DisplayMode mode();

// Language code of the loaded table, or "en" if none
const wxString& language();

// Translates [text], returning it unchanged if there is no translation
wxString tr(const wxString& text);
wxString tr(const char* text);
wxString tr(const wchar_t* text);
wxString tr(const string& text);

// Same as tr(), but returns the result as a UTF-8 std::string
string trs(string_view text);

} // namespace slade::i18n
