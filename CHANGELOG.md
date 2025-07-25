# materialshaderreplacer-changelog

## 1.3.0-next.1

### Patch Changes

- Improved how localization is initialized in the Unity Editor to ensure translations are loaded correctly when the editor starts. ([`f6b2731`](https://github.com/hazre/MaterialShaderReplacer/commit/f6b273196212d956b1e4b052d035aeeb740a6138))

- Updated Non-Destructive Modular Framework to version ^1.8.0 to ensure compatibility with the latest features and improvements. ([`4b7e06c`](https://github.com/hazre/MaterialShaderReplacer/commit/4b7e06c23ad62191c65f16b12234d44b3c232e0b))

## 1.3.0-next.0

### Minor Changes

- Added "Use Invalid Materials as Source" option to automatically target and replace materials with invalid/missing shaders. ([`ec9fd6c`](https://github.com/hazre/MaterialShaderReplacer/commit/ec9fd6cbe69c94470ce0074b9c52d7c94d9b57a7))

## 1.2.3

### Patch Changes

- Fixed setting invalid language id to NDMF's language preferences. ([`d800719`](https://github.com/hazre/MaterialShaderReplacer/commit/d800719b550ea9e8f621977f48bd6e21c502db03))

## 1.2.2

### Patch Changes

- Fixed localization files not loading. ([`6fff238`](https://github.com/hazre/MaterialShaderReplacer/commit/6fff238cb9f73456a9166ea85275d98f8bc95cb6))

## 1.2.1

### Patch Changes

- Implemented a custom language picker that only displays languages explicitly supported by the package, preventing display of other NDMF registered languages and resolving issue #1. ([`0d59e41`](https://github.com/hazre/MaterialShaderReplacer/commit/0d59e41b55717855d5a9acf35bdb0e42fc52a1b6))

## 1.2.0

### Minor Changes

- **✨ Features & Enhancements**

  - **New "Use All Materials as Source" Option**: Added a powerful feature under a collapsible "Advanced Options" section (collapsed by default). This allows users to target _all_ materials within the selected scope for shader replacement, overriding specific source shader selections. (Requested by @Mawco because they like breaking their projects apparently 🤷) ([`ac4e7ce`](https://github.com/hazre/MaterialShaderReplacer/commit/ac4e7ce1aff21b2edd47a5c20f961a674a134079))
  - **Localization Support**: Integrated localization capabilities, enabling the tool to be translated into multiple languages.
    - Initial support includes Japanese (thanks to @rassi0429) and German. ([`f14b55f`](https://github.com/hazre/MaterialShaderReplacer/commit/f14b55f788a73a890f2de29385188ae706e2e5ff))

- **🛠️ Developer Experience**

  - **Changesets Integration**: Integrated the `changesets` tool for streamlined, automated versioning and changelog generation, improving the release process. ([`b9a4cf4`](https://github.com/hazre/MaterialShaderReplacer/commit/b9a4cf47bd765a5bb921fd11851f6b93972fd939))
