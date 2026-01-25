# Universal Drive

## Problem
Driving arbitrary 3D objects using traditional vehicle physics leads to instability,
uncontrollable drifting, and frequent flipping.

## Design Goal
Provide an arcade-style vehicle controller that prioritizes player control over realism.

## Core Idea
This system intentionally dominates and reshapes physics forces rather than simulating real-world vehicle behavior.

## Current State
- Architecture and control loop established
- Input abstraction implemented
- Initial force application in place

## Known Challenges (WIP)
- Ground detection across arbitrary shapes
- Preventing tall object tipping
- Lateral velocity control without killing responsiveness
