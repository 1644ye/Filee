# Filee SVG Drag (Chrome)

Companion to Filee's Ctrl-drag conversion. It supports ordinary pictures, linked image cards, and inline SVG elements. Hold Ctrl to prepare the pointed picture for a native image drag. For ordinary images it uses the highest-resolution URL supplied in the image's srcset, keeping URL parameters intact. SVG is serialized with its drawing and styles.

## Install and use

1. Open `chrome://extensions`, enable Developer mode, and choose **Load unpacked**.
2. Select this `browser-extension` directory. Keep it in place while the extension is installed.
3. Allow the extension to run on all websites. It activates automatically on HTTP and HTTPS pages.
4. Select complete SVG source code and hold **Ctrl** while dragging it to Filee. The companion supplies native text drag data on webpages that do not make selections draggable. Enable or disable this in Filee Settings > Shortcuts > Wake Filee by dragging SVG code (enabled by default). Chrome internal pages and DevTools cannot be modified by the companion.
5. Hold **Ctrl**, move onto a picture or SVG and drag it into Filee's conversion ring.

After updating, reload the extension in `chrome://extensions` and accept Chrome's expanded site-access prompt
if shown. Refresh already-open webpages, or click **Filee SVG Drag** once to activate the current tab.
No per-site activation is needed. Chrome internal pages and the Web Store cannot be enabled.
Remove the extension in `chrome://extensions` to uninstall it.

Choose **Download** in the ring to save without conversion, or select a conversion format as before. In Settings > General > Download save location, choose the system Downloads folder, a specified folder, or a folder picker for each download. Cancelling the picker saves nothing. Download keeps the file even when Save only converted files is enabled.

## What is preserved

- Complete paths, viewBox, dimensions, nested SVGs, inherited colors and common computed paint/text styles.
- Same-page referenced symbols, paths, gradients, masks and clipping definitions, copied into the SVG.
- The result is SVG, not a screenshot; output transparency is preserved.

External images, external SVG resources, web fonts, foreignObject and animation are not captured. Cross-frame
and closed-shadow-root SVGs are not supported. For an unsupported external reference the extension does not
start the SVG drag. Selected source code must still include the complete drawing; missing paths cannot be
reconstructed from an empty tag.

## Permissions and data

No server, downloads, clipboard access, native messaging or background page scraping. The extension has
access to all HTTP(S) websites and automatically installs its drag listener in each top-level page.
`scripting` also supports already-open tabs and removes old per-site registrations during an update.
It reads the pointed image URL, or SVG computed styles
and referenced definitions only when Ctrl is held. It transfers them locally through native drag data.

## Regression checks

Serve the repository root over localhost and open `tests/browser-extension/svg-drag.html` and `tests/browser-extension/image-drag.html` in a browser.
The page exercises the production script with inherited styles, referenced definitions, drag formats and
cleanup. A native Chrome-to-Filee drag is a separate integration check.

## Direct video downloads
Version 0.5.0 also supports Ctrl-drag on HTML video elements with HTTP(S) file sources. Choose Download in Filee. URL query parameters are preserved. Blob/MediaSource streams, HLS/DASH playlists, DRM and authenticated site-specific extraction are not supported. Mobile touch emulation still requires Mobile (no touch) for native dragging.

