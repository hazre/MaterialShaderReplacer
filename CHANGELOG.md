# materialshaderreplacer-changelog

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
