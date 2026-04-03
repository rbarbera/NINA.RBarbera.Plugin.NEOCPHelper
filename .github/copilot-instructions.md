# NEOCP Helper Plugin - Copilot Instructions

This is a C# plugin for NINA (Nighttime Imaging 'N' Astronomy) that helps users automate observation sequences for Near-Earth Objects on the Minor Planet Center's NEOCP list.

## Build & Test

### Build
```bash
dotnet restore
dotnet build "NINA.RBarbera.Plugin.NEOCPHelper.sln" -c Release
```

### Post-Build
The project includes a post-build step that automatically copies the compiled DLL to the local NINA plugins directory (`%localappdata%\NINA\Plugins\3.0.0\NEOCP Helper\`). This happens automatically on local builds.

### Release & Publication
Release builds are triggered by pushing a Git tag in format `X.Y.Z.W` (e.g., `1.0.0.0`). The GitHub Actions workflow:
1. Builds the project
2. Packages the DLL and PDB files
3. Creates a release with the built artifacts
4. Publishes manifest to the NINA plugin registry (if the manifest repository exists)

See `.github/workflows/github-action.yaml` for details.

## Architecture

### Core Components

**Plugin Entry Point** (`NeocpHelper.cs`)
- Implements `IPluginManifest` to expose the plugin to NINA
- Uses dependency injection for mediators (sequence, camera)
- Manages global plugin settings via `Properties.Settings`

**Sequencer Container** (`Sequencer/Containers/NEOCPListContainer.cs`)
- Custom NINA sequencer container that displays a list of NEOCP objects
- Implements `IDeepSkyObjectContainer` for NINA integration
- Handles session creation, sorting by various parameters (magnitude, declination, score, etc.)
- Manages object selection and session management

**Sequencer Instruction** (`Sequencer/Instructions/UpdateNEOEphemerides.cs`)
- Must be included in session templates to work
- Fetches latest ephemeris data from MPC for a given object
- Calculates:
  - Maximum exposure time per shot (respecting max pixel trace length)
  - Maximum session duration (ensuring NEO stays within sensor bounds)
  - Optimal field center coordinates
- Updates container coordinates, exposure time, and loop duration parameters

**Data Models** (`Models/`)
- `NEOCPTarget`: Represents a NEO with session info and coordinates
- `NEOCPEphemeride`: Single ephemeris point (time, RA, Dec, velocity)
- `NEOCPField`: Represents the observable field geometry
- `NEOCPInputTarget`: Input format for NEOCP data
- `NEOCPDeepSkyObject`: NINA integration wrapper

**Utilities** (`Utility/`)
- `NEOCPScanner`: HTML parser for NEOCP ephemeris data (custom string reader that extracts from `<pre>` tags)
- `MPCDownloader`: HTTP client for fetching ephemeris data from Minor Planet Center
- `MPCScanner`: Parses MPC ephemeris format (time, RA/Dec as strings/decimals, velocity)
- `AstroUtilExtension`: Helper methods for astrometric calculations (coordinate conversions, angular distance, position angle, etc.)
- `MathUtil`: Mathematical utilities (trigonometry, conversions)
- `ItemUtil`: General utilities for collections and parsing
- `Converters`: XAML value converters for WPF UI

**UI** (`View/`, `Options.xaml`)
- `Options.xaml`: Global plugin settings UI (filter ranges, sensor parameters, template selection)
- `AltitudeChart.xaml.cs`: Visualizes object altitude over time
- `NeocpHelperTemplates.xaml.cs`: Template management for session creation

## Key Conventions

### Namespace Organization
- Root: `NINA.RBarbera.Plugin.NeocpHelper`
- Sequencer items: `NINA.RBarbera.Plugin.NeocpHelper.Sequencer.*` (containers and instructions)
- Models: `NINA.RBarbera.Plugin.NeocpHelper.Models`
- Utilities: `NINA.RBarbera.Plugin.NeocpHelper.Utility`

### NINA Integration Points
- **Mediators** via MEF constructor injection: `ISequenceMediator`, `ICameraMediator`, `IProfileService`, etc.
- **Export Metadata** for discoverable sequence items:
  - `Name`: Display name in UI
  - `Description`: Tooltip description
  - `Icon`: SVG icon name (e.g., "ImpactorSVG")
  - `Category`: Grouping in sequence editor (use "NEOCP Helper" for plugin-specific items)
- **Serialization**: Uses `JsonObject(MemberSerialization.OptIn)` to serialize only explicitly marked properties

### Data Flow for Session Creation
1. User selects NEO from list in `NEOCPListContainer`
2. Container generates a new session from configured template
3. `UpdateNEOEphemerides` instruction (in template) fetches latest ephemeris when executed
4. Ephemeris calculations update:
   - Container target coordinates (RA, Dec, altitude)
   - Exposure time in `TakeExposure` instruction (never exceeds template max)
   - Duration in `LoopForTimeSpan` instruction (never exceeds template max)
5. `SlewAndCenter` and `TakeExposure` instructions use the updated values

### Important Behavior
- **UpdateNEOEphemerides respects maximums**: If template has 1hr duration, it won't extend beyond 1hr even if NEO stays in field longer
- **Object filtering**: Uses NEOCP standard parameters (V magnitude range, declination range, score range)
- **Observer coordinates**: Takes from NINA profile, not NEOCP observatory codes, to avoid duplication
- **Ephemeris caching**: Not cached—always fetches fresh data at execution time to handle orbital element updates

### Settings
- Global plugin settings stored in `Properties.Settings` (autogenerated from `Properties\Settings.Settings`)
- User can set: magnitude/declination/score filters, max trace pixel length, sensor area usage %, target name prefix, template selection
- Settings persisted via NINA's standard mechanism

### Code Style
- Follow `.editorconfig` conventions: 4-space indentation, CRLF line endings, no final newline
- Use explicit types (not `var`) for most declarations
- Interfaces prefixed with `I` (e.g., `ISequenceItem`, `IValidatable`)
- Namespace declarations use block-scoped style
- Pattern matching and switch expressions preferred where applicable

## Common Tasks

### Adding a New Sequencer Instruction
1. Create class in `Sequencer/Instructions/` inheriting from `SequenceItem`
2. Apply export metadata and `[Export]` attributes
3. Use `[ImportingConstructor]` for dependency injection
4. Implement `IValidatable` if it has configuration to validate
5. Override `Execute()` for the main logic

### Extending Data Models
- Add properties to Models and ensure JSON serialization attributes are updated
- Update parsers (`NEOCPScanner`, `MPCScanner`) if adding new data sources

### Updating UI
- XAML files use WPF and NINA's standard controls
- Value converters in `Utility/Converters.cs` handle data transformation for display
- Plugin options template must be named with pattern `NeocpHelper_Options`
