# Material Shader Replacer

Batch replace shaders on materials in your Unity project.

<a href="https://hazre.github.io/vpm-listing"><img alt="Add to VCC / ALCOM" src="https://img.shields.io/badge/-Add%20to%20VCC%20\%20ALCOM-%232baac1?style=for-the-badge"></a>

Quickly swap one shader for another across many materials. Useful for upgrades or bulk changes.



https://github.com/user-attachments/assets/deb91184-b3dd-4b61-9956-053af6664c37



## Features

*   **Easy Shader Swapping:** Quickly find all materials using a specific shader and change them to another.
*   **Familiar Shader Picker:** Select shaders using the same organized dropdown menu you see in Unity's Material Inspector.
*   **Targeted Changes:**
    *   Process your **entire project** at once.
    *   Or, limit changes to a **specific GameObject and all objects nested under it**.
*   **Skip Specific GameObjects:** Exclude certain GameObjects (and their children) from shader changes when working within a hierarchy.
*   Supports Unity's **Undo** functionality for most changes.
*   **Localization Support:** The tool supports multiple languages (English, Japanese, German).

## Installation

Use [this link](https://hazre.github.io/vpm-listing) to add the repository to VCC.
Then add `Material Shader Replacer` package to your projects.

## How to Use

1.  **Open the Tool:**
    *   In the Unity Editor menu bar, navigate to `Tools` > `Material Shader Replacer`.

2.  **Set Processing Scope:**
    *   Choose **"Entire Project"** to process all material assets.
    *   Choose **"GameObject and Children"** and then assign a **"Root GameObject"** to limit the scope.

3.  **Select Shaders:**
    *   **Source Shader:** From the dropdown, select the shader you want to find and replace.
    *   **Target Shader:** From the dropdown, select the new shader you want to apply.

4.  **(Optional) Configure Exclusions:**
    *   If using "GameObject and Children" scope, you can add GameObjects to the **"Excluded GameObjects"** list. These objects and their children will be skipped.

5.  **Run Replacement:**
    *   Click the **"Replace Shaders"** button.
    *   A confirmation dialog will appear. Read it carefully.

6.  **Review Log:**
    *   Once completed, a summary dialog will appear.
    *   The tool window will display a log listing all materials that were changed or skipped.

## Heads Up!

*   **⚠️ ALWAYS BACKUP YOUR PROJECT FIRST!** This tool modifies assets. (Or better yet, version control your project, so you can always rollback)
*   It only changes the shader reference, **not** its properties (textures, colors, etc.). You'll need to adjust those manually if the new shader has different property names or types.

## Credits

* Localization system is based on code from [anatawa12's AvatarOptimizer](https://github.com/anatawa12/AvatarOptimizer).
* Uses [Non-Destructive Modular Framework (NDMF)](https://github.com/bdunderscore/ndmf) as a dependency for localization.
