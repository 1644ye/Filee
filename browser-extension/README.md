# Filee SVG Drag (Chrome)

Companion to Filee's Ctrl-drag conversion. Websites often use inline SVG elements that are not natively
draggable. This extension prepares a standalone SVG under the pointer while Ctrl is held, then lets Chrome
perform a normal image drag. Filee's donut, presets and output settings are unchanged.

## Install and use

1. Open `chrome://extensions`, enable Developer mode, and choose **Load unpacked**.
2. Select this `browser-extension` directory. Keep it in place while the extension is installed.
3. Open the website, click **Filee SVG Drag** in Chrome's extensions menu, and approve access to that site.
4. Hold **Ctrl**, move onto an SVG picture and drag it into Filee's conversion ring.

Enabling a site is a one-time action. Registered content scripts persist across browser restarts. Reload
other tabs that were already open on that site. Chrome internal pages and the Web Store cannot be enabled.
Remove the extension in `chrome://extensions` to uninstall it.

## What is preserved

- Complete paths, viewBox, dimensions, nested SVGs, inherited colors and common computed paint/text styles.
- Same-page referenced symbols, paths, gradients, masks and clipping definitions, copied into the SVG.
- The result is SVG, not a screenshot; output transparency is preserved.

External images, external SVG resources, web fonts, foreignObject and animation are not captured. Cross-frame
and closed-shadow-root SVGs are not supported. For an unsupported external reference the extension does not
start the SVG drag. Selected source code must still include the complete drawing; missing paths cannot be
reconstructed from an empty tag.

## Permissions and data

No server, downloads, clipboard access, native messaging or background page scraping. The extension requests
only the current HTTP(S) origin when its toolbar button is clicked. `activeTab` and `scripting` enable injection
on that site; optional host access keeps it enabled on later visits. It reads the pointed SVG, computed styles
and referenced definitions only when Ctrl is held. It transfers them locally through native drag data.

## Regression checks

Serve the repository root over localhost and open `tests/browser-extension/svg-drag.html` in a browser.
The page exercises the production script with inherited styles, referenced definitions, drag formats and
cleanup. A native Chrome-to-Filee drag is a separate integration check.
