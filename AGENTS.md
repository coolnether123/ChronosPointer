# Chronos Pointer Agent Guide

Own only this repository. Read `README.md`, package metadata, project files,
source architecture, settings, and compatibility code before editing.

Keep schedule rendering, time-state calculation, incident integration,
compatibility, settings, and localization separated. Preserve save
compatibility and existing player settings. Adopt standalone Spine only for an
explicit migration of generalized facilities; do not create an incidental
runtime dependency.

Derive build and test entrypoints from the current project and use the shared
runtime/package workflow in `A:\Dev\RimWorld\AGENTS.md`. UI changes require an
isolated game lane with its owned logs and captures.
