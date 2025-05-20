# Material Shader Replacer

Batch replace shaders on materials in your Unity project.

<a href="vcc://vpm/addRepo?url=https://hazre.github.io/vpm-listing/index.json"><img alt="Add to VCC / ALCOM" src="https://img.shields.io/badge/-Add%20to%20VCC%20\%20ALCOM-%232baac1?style=for-the-badge"></a>

Quickly swap one shader for another across many materials. Useful for upgrades or bulk changes.

## Installation

Use [this link](https://hazre.github.io/vpm-listing) to add the repository to VCC.
Then add `Material Shader Replacer` package to your projects.

## How to Use

1.  Open: `Tools` > `Material Shader Replacer`.
2.  Pick **Source Shader** (the one to replace).
3.  Pick **Target Shader** (the new one).
4.  Click **"Replace Shaders in Project"**. Confirm.
5.  Check the log for changed materials.

## Heads Up!

  *  **⚠️ ALWAYS BACKUP YOUR PROJECT FIRST!** This tool modifies assets. (Or better yet, version control your project, so you can always rollback)
  *  It only changes the shader, **not** its properties (textures, colors, etc.). You'll need to adjust those manually if the new shader is different.