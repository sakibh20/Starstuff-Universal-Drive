# 🚗 Universal Drive — Arcade Vehicle Controller (Unity 6.3)

## Overview

**Universal Drive** is an arcade-style vehicle controller designed to make *any arbitrary 3D object* drivable and fun — whether it’s a car, a crate, or something tall and weird.

This project was built as a response to the **“Universal Drive Fix”** task.
The goal was **not realism**, but **predictable, grippy, responsive handling** that works across objects of very different shapes and sizes — similar in spirit to *Mario Kart* or *Rocket League*.

---

## 🎯 Problem Statement

In *Starstuff*, players can attach a vehicle component to any object.
However, the existing physics feel like driving on ice:

* Excessive drifting
* Easy spin-outs
* Tall objects flip too easily
* Hard to recover after flipping

It’s funny for a few seconds — but frustrating for gameplay.

---

## ✅ Solution Summary

This project implements a **custom arcade-style vehicle controller** built *from scratch* on top of **Unity PhysX**, without using `WheelCollider` or third-party vehicle frameworks.

### Core Design Goals

* Works with **arbitrary object shapes**
* **Grippy and snappy** handling
* **Hard to flip on flat ground**
* **Auto-corrects while side flipping**
* Same script for all vehicles (no per-vehicle tuning)

---

## 🧠 Key Techniques Used

### 1. Physics-Based, Not Kinematic

* Uses **Unity PhysX** for:

    * Gravity
    * Collision resolution
    * Integration
* No manual velocity teleporting
* No kinematic hacks

---

### 2. No WheelCollider (Intentional)

WheelCollider was deliberately **not used** because:

* It assumes a car-like shape
* It becomes unreliable for arbitrary objects (banana, crate, tall shapes)
* It hides a lot of behavior behind opaque parameters
* It is harder to adapt for “anything can be a vehicle”

Instead, this controller:

* Applies forces and torques directly
* Shapes behavior via grip, stabilization, and steering authority

---

### 3. Ground Detection (Shape-Agnostic)

* Raycast-based grounding from **center of mass**
* Ray length derived from **renderer bounds**
* Ground normal tracked for future extensibility

Result:

* Vehicles remain stable on flat ground
* Tall objects don’t instantly topple

---

### 4. Lateral Grip System (Anti-Ice Feel)

Sideways velocity is actively corrected while grounded:

* Projects velocity onto forward axis
* Applies corrective force proportional to grip
* Grip scales with speed

Result:

* Reduced drifting
* Predictable cornering
* Arcade-style “snap” without being rigid

---

### 5. Upright Stabilization & Flip Recovery

* Prevents sideways rolling on flat surfaces
* Allows flipping on slopes or extreme situations
* Does **not** forcibly snap the vehicle upright

Observed behavior:

* ✅ Almost never flips sideways
* ⚠️ Can flip on slopes (known limitation)
* ⚠️ Can flip on slopes (known limitation)
* ️⚠️ Can flip on its head (Couldn't solve completely)

---

### 6. Unified Controller for All Vehicles

* **Same `UniversalVehicleController` script**
* No per-vehicle parameters
* Vehicle differences come purely from:

    * Mesh shape
    * Collider setup
    * Center of mass calculation

---

## 🎮 Input Support

### Desktop

* WASD / Arrow keys
* Analog-style steering response

### Mobile

* Virtual joystick support
* Forward + reverse detection
* Dead zones and curves applied
* **Not fully tuned yet** (see Limitations)

> ⚠️ Mobile input has not been tested on an actual mobile build — the system exists, but tuning would benefit from real-device testing.

---

## 📦 Project Structure

* `UniversalVehicleController`
  → Main vehicle logic (shared by all vehicles)

* `VehicleContext`
  → Runtime state (speed, grip, grounded, etc.)

* Modular subsystems:

    * `GroundDetector`
    * `LateralGrip`
    * `UprightStabilization`
    * `Downforce`
    * `CenterOfMassAdjuster`

* `InputManager`

    * Switches between keyboard and mobile input at runtime
    * Allows vehicle switching at runtime

---

## 🧪 Test Scene

The included test scene contains:

* **At least 3 vehicles**
* Very different shapes and sizes
* All driven using **the same controller script**
* No manual tuning differences

Some prefab setup is required:

* Add appropriate colliders
* Adjust collider shapes
* Make prefabs

No controller-side customization is required per vehicle.

---

## 🎥 Demo Video

## 🎥 Demo Video

[![Universal Drive – Demo Video](https://img.youtube.com/vi/_wc-7pQP9fc/0.jpg)](https://youtu.be/_wc-7pQP9fc)

The video demonstrates:

- Different vehicles driving with the same controller
- Input Switch
- Stability on flat ground
- Reduced drifting
- Flip recovery behavior
- Fail Scenario

---

## ⚠️ Known Limitations

Being transparent and honest:

* Slope handling can still cause flips
* Mobile joystick control is **not perfect**
* No mobile build testing was performed
* Requires reasonable collider setup per vehicle prefab
* Reverse behavior on mobile is basic
* Flipped vehicle doesn't recover

These are acknowledged trade-offs given the scope and time constraints.
Could have solved some more issues but due to other responsibilies couldn't invest more time on it.

---

## 🛠️ Tools & Technologies Used

* **Unity 6.3**
* **C#**
* **Unity PhysX**
* No third-party libraries
* No vehicle frameworks
* No WheelCollider

---

## 🤔 Alternatives Considered (and Why Not Used)

### WheelCollider

* Assumes wheels
* Breaks for arbitrary shapes
* Harder to customize arcade behavior

### Third-Party Vehicle Controllers

* Come with large amounts of unrelated code
* Difficult to adapt to “anything is a vehicle”
* Harder to reason about and debug

Building from scratch provided:

* Full control
* Clear behavior
* Easier extensibility

---

## 🤖 Declaration of LLM Use

Large Language Models (LLMs) were used **as an assistance tool** during development for:

* Coding
* Idea Gen
* Architectural discussion
* Documentation drafting

---
