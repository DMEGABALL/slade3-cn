// -----------------------------------------------------------------------------
// SLADE - It's a Doom Editor
// Copyright(C) 2008 - 2026 Simon Judd
//
// Email:       sirjuddington@gmail.com
// Web:         http://slade.mancubus.net
// Filename:    Translation.cpp
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


// -----------------------------------------------------------------------------
//
// Includes
//
// -----------------------------------------------------------------------------
#include "Main.h"
#include "Translation.h"
#include "Application/App.h"
#include "General/Log.h"
#include <wx/file.h>
#include <unordered_map>

using namespace slade;


// -----------------------------------------------------------------------------
//
// Variables
//
// -----------------------------------------------------------------------------
namespace
{
struct Entry
{
	string      text;
	i18n::DisplayMode mode    = i18n::DisplayMode::Bilingual;
	bool        has_mode = false;
};

std::unordered_map<string, Entry> table;
i18n::DisplayMode                 display_mode = i18n::DisplayMode::Bilingual;
bool                              is_active    = false;
wxString                          lang_code    = wxS("en");

// -----------------------------------------------------------------------------
// Removes leading/trailing whitespace from [str]
// -----------------------------------------------------------------------------
string trimmed(const string& str)
{
	const auto begin = str.find_first_not_of(" \t\r\n");
	if (begin == string::npos)
		return {};

	const auto end = str.find_last_not_of(" \t\r\n");
	return str.substr(begin, end - begin + 1);
}

// -----------------------------------------------------------------------------
// Expands the escape sequences \n, \t and \\ in [str]
// -----------------------------------------------------------------------------
string unescape(const string& str)
{
	string out;
	out.reserve(str.size());

	for (size_t a = 0; a < str.size(); a++)
	{
		if (str[a] == '\\' && a + 1 < str.size())
		{
			if (str[a + 1] == 'n')
			{
				out += '\n';
				a++;
				continue;
			}
			if (str[a + 1] == 't')
			{
				out += '\t';
				a++;
				continue;
			}
			if (str[a + 1] == '\\')
			{
				out += '\\';
				a++;
				continue;
			}
		}

		out += str[a];
	}

	return out;
}

// -----------------------------------------------------------------------------
// Parses a display mode name. Returns false if [str] is not a valid mode.
// -----------------------------------------------------------------------------
bool parseMode(const string& str, i18n::DisplayMode& mode)
{
	if (str == "zh" || str == "chinese")
	{
		mode = i18n::DisplayMode::Chinese;
		return true;
	}
	if (str == "both" || str == "bilingual")
	{
		mode = i18n::DisplayMode::Bilingual;
		return true;
	}
	if (str == "en" || str == "english")
	{
		mode = i18n::DisplayMode::English;
		return true;
	}

	return false;
}

// -----------------------------------------------------------------------------
// Reads the entire contents of [path] into [data]. Returns false on failure.
// -----------------------------------------------------------------------------
bool readTextFile(const string& path, string& data)
{
	wxFile file;
	if (!file.Open(wxString::FromUTF8(path), wxFile::read))
		return false;

	const auto length = file.Length();
	if (length <= 0)
		return false;

	data.resize(static_cast<size_t>(length));
	const auto read = file.Read(&data[0], data.size());
	data.resize(read);
	file.Close();

	// Skip UTF-8 BOM
	if (data.size() >= 3 && static_cast<unsigned char>(data[0]) == 0xEF
		&& static_cast<unsigned char>(data[1]) == 0xBB && static_cast<unsigned char>(data[2]) == 0xBF)
		data = data.substr(3);

	return true;
}

// -----------------------------------------------------------------------------
// Loads the translation table from [path]. Returns true on success.
// -----------------------------------------------------------------------------
bool loadTable(const string& path)
{
	string data;
	if (!readTextFile(path, data))
		return false;

	table.clear();

	size_t pos = 0;
	while (pos <= data.size())
	{
		auto end = data.find('\n', pos);
		if (end == string::npos)
			end = data.size();

		auto line = data.substr(pos, end - pos);
		pos       = end + 1;

		if (!line.empty() && line.back() == '\r')
			line.pop_back();

		// Comments & blank lines
		const auto trimmed_line = trimmed(line);
		if (trimmed_line.empty() || trimmed_line[0] == '#')
			continue;

		// Directives
		if (trimmed_line[0] == '@')
		{
			auto       directive = trimmed_line.substr(1);
			const auto space     = directive.find_first_of(" \t");
			auto       name      = (space == string::npos) ? directive : directive.substr(0, space);
			auto       value     = (space == string::npos) ? string{} : trimmed(directive.substr(space + 1));

			if (name == "mode")
			{
				i18n::DisplayMode mode;
				if (parseMode(value, mode))
					display_mode = mode;
				else
					log::warning("i18n: Unknown display mode \"{}\" in {}", value, path);
			}

			continue;
		}

		// Find the (first) tab separating source text and translation
		const auto tab = line.find('\t');
		if (tab == string::npos)
			continue;

		const auto key = unescape(line.substr(0, tab));
		if (key.empty())
			continue;

		Entry entry;
		auto  rest = line.substr(tab + 1);

		// Optional third field: display mode override
		const auto tab2 = rest.find('\t');
		if (tab2 != string::npos)
		{
			i18n::DisplayMode mode;
			if (parseMode(trimmed(rest.substr(tab2 + 1)), mode))
			{
				entry.mode     = mode;
				entry.has_mode = true;
			}
			rest = rest.substr(0, tab2);
		}

		entry.text     = unescape(rest);
		table[key]     = entry;
	}

	return !table.empty();
}
} // namespace


// -----------------------------------------------------------------------------
//
// Translation Functions
//
// -----------------------------------------------------------------------------
namespace slade::i18n
{
// -----------------------------------------------------------------------------
// Loads the translation table for [lang], searching the usual SLADE data
// directories. Returns true if a table was loaded.
// -----------------------------------------------------------------------------
bool init(string_view lang)
{
	is_active    = false;
	lang_code    = wxString::FromUTF8(string{ lang });
	display_mode = DisplayMode::Bilingual;

	if (lang.empty() || lang == "en")
		return false;

	const string file = fmt::format("lang/{}.txt", lang);
	const string dirs[] = { app::path(file, app::Dir::Executable),
		                    app::path(file, app::Dir::Resources),
		                    app::path(file, app::Dir::Data),
		                    app::path(file, app::Dir::User) };

	for (const auto& dir : dirs)
	{
		if (!wxFileExists(wxString::FromUTF8(dir)))
			continue;

		if (loadTable(dir))
		{
			is_active = true;
			log::info("Loaded translation table \"{}\" ({} entries)", dir, table.size());
			return true;
		}

		log::warning("i18n: Failed to parse translation table \"{}\"", dir);
	}

	log::info("i18n: No translation table found for \"{}\", using English", lang);
	return false;
}

// -----------------------------------------------------------------------------
// Returns true if a translation table has been loaded
// -----------------------------------------------------------------------------
bool active()
{
	return is_active;
}

// -----------------------------------------------------------------------------
// Returns the display mode currently in use
// -----------------------------------------------------------------------------
DisplayMode mode()
{
	return display_mode;
}

// -----------------------------------------------------------------------------
// Returns the language code of the loaded table, or "en" if none
// -----------------------------------------------------------------------------
const wxString& language()
{
	return lang_code;
}

// -----------------------------------------------------------------------------
// Translates [text] to a UTF-8 std::string
// -----------------------------------------------------------------------------
string trs(string_view text)
{
	if (!is_active || table.empty() || text.empty())
		return string{ text };

	const auto it = table.find(trimmed(string{ text }));
	if (it == table.end())
		return string{ text };

	const auto& entry = it->second;
	auto        mode  = entry.has_mode ? entry.mode : display_mode;

	if (mode == DisplayMode::English)
		return string{ text };

	if (mode == DisplayMode::Chinese)
		return entry.text;

	// Bilingual: "翻译 (English)"
	if (entry.text.empty())
		return string{ text };

	return fmt::format("{} ({})", entry.text, text);
}

// -----------------------------------------------------------------------------
// Translates [text]
// -----------------------------------------------------------------------------
wxString tr(const wxString& text)
{
	if (!is_active)
		return text;

	return wxString::FromUTF8(trs(text.utf8_string()));
}

// -----------------------------------------------------------------------------
// Translates [text]
// -----------------------------------------------------------------------------
wxString tr(const char* text)
{
	if (!is_active || !text)
		return wxString::FromUTF8(text ? text : "");

	return wxString::FromUTF8(trs(text));
}

// -----------------------------------------------------------------------------
// Translates [text]
// -----------------------------------------------------------------------------
wxString tr(const wchar_t* text)
{
	if (!is_active || !text)
		return wxString(text ? text : L"");

	return wxString::FromUTF8(trs(wxString(text).utf8_string()));
}

// -----------------------------------------------------------------------------
// Translates [text]
// -----------------------------------------------------------------------------
wxString tr(const string& text)
{
	if (!is_active)
		return wxString::FromUTF8(text);

	return wxString::FromUTF8(trs(text));
}
} // namespace slade::i18n
