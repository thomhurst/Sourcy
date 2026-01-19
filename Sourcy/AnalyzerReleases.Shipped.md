; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SOURCY001 | Sourcy | Warning | Repository root not found
SOURCY002 | Sourcy | Info | File skipped during generation
SOURCY003 | Sourcy | Warning | Error during source generation
SOURCY004 | Sourcy | Warning | Git command failed
SOURCY005 | Sourcy | Warning | Git is not available
SOURCY006 | Sourcy | Disabled | Invalid identifier sanitized
SOURCY007 | Sourcy | Info | Path too long
SOURCY008 | Sourcy | Info | Access denied
SOURCY009 | Sourcy | Warning | Invalid path characters
SOURCY010 | Sourcy | Warning | Invalid custom root path
SOURCY011 | Sourcy | Disabled | Symlink cycle detected
SOURCY012 | Sourcy | Disabled | Maximum directory depth reached
SOURCY013 | Sourcy | Disabled | Relative path calculation used fallback
SOURCY014 | Sourcy | Disabled | Cloud placeholder file skipped
SOURCY015 | Sourcy | Warning | Unexpected error during source generation
SOURCY016 | Sourcy | Warning | Project directory not available
SOURCY100 | Sourcy | Disabled | Generation successful
SOURCY101 | Sourcy | Info | Fallback value used
SOURCY102 | Sourcy | Info | Shallow clone detected
SOURCY103 | Sourcy | Warning | UNC/Network path detected
SOURCY104 | Sourcy | Info | Git submodule detected
SOURCY105 | Sourcy | Info | Custom root path used
