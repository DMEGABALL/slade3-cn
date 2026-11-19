# wxWidgets version minimum 3.0
if (NOT WX_VERSION)
	SET(WX_VERSION 3.0)
endif (NOT WX_VERSION)

# Sources
file(GLOB_RECURSE SLADE_SOURCES CONFIGURE_DEPENDS
	Application/*.cpp
	Archive/*.cpp
	Audio/*.cpp
	Database/*.cpp
	Game/*.cpp
	General/*.cpp
	Geometry/*.cpp
	Graphics/*.cpp
	MainEditor/*.cpp
	MapEditor/*.cpp
	OpenGL/*.cpp
	SLADEMap/*.cpp
	TextEditor/*.cpp
	TextureEditor/*.cpp
	UI/*.cpp
	Utility/*.cpp
	ZReaders/*.cpp
)

# Lua/Scripting sources if not excluded
if (NOT NO_LUA)
	file(GLOB_RECURSE SLADE_SCRIPTING_SOURCES CONFIGURE_DEPENDS Scripting/*.cpp)
	set(SLADE_SOURCES ${SLADE_SOURCES} ${SLADE_SCRIPTING_SOURCES})
else ()
	ADD_DEFINITIONS(-DNO_LUA)
endif ()

# Disable implicit wxString encoding
# (only when building wx 3.3 currently, as it causes build errors in wx 3.2)
if (BUILD_WX)
	ADD_DEFINITIONS(-DwxNO_IMPLICIT_WXSTRING_ENCODING)
endif ()

# Headers
file(GLOB_RECURSE SLADE_HEADERS CONFIGURE_DEPENDS
	*.h
	*.hpp
)

# Setup depending on platform
if (WIN32 AND MSVC)
	include("win_msvc") # Windows+MSVC
else ()
	include("unix") # Linux or MacOS
endif ()

# ------------------------------------------------------------------------------
# Chinese localisation
#
# The translation table (lang/<language>.txt) is loaded at runtime from the
# executable's directory. Copy the language files next to the built binary so a
# freshly built SLADE is already localised.
# ------------------------------------------------------------------------------
if (EXISTS "${CMAKE_SOURCE_DIR}/lang")
	add_custom_command(
		TARGET slade POST_BUILD
		COMMAND ${CMAKE_COMMAND} -E copy_directory
			"${CMAKE_SOURCE_DIR}/lang"
			"$<TARGET_FILE_DIR:slade>/lang"
		COMMENT "Copying lang/ next to the executable"
		VERBATIM)
endif ()
