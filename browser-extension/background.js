// Site access is requested only after the user clicks the toolbar button, for that site's origin.
chrome.action.onClicked.addListener(async tab => {
  try {
    const url = new URL(tab.url);
    if (!/^https?:$/.test(url.protocol)) throw new Error("Open a normal website first.");
    const origin = `${url.origin}/*`;
    if (!await chrome.permissions.request({ origins: [origin] })) return;
    const id = `filee-${encodeURIComponent(url.origin)}`;
    const registered = await chrome.scripting.getRegisteredContentScripts({ ids: [id] });
    if (!registered.length) await chrome.scripting.registerContentScripts([{
      id, matches: [origin], js: ["svg-drag.js"], runAt: "document_idle", persistAcrossSessions: true
    }]);
    await chrome.scripting.executeScript({ target: { tabId: tab.id }, files: ["svg-drag.js"] });
    await chrome.action.setBadgeText({ tabId: tab.id, text: "ON" });
    await chrome.action.setTitle({ tabId: tab.id, title: "Hold Ctrl and drag an SVG picture to Filee" });
  } catch (error) {
    await chrome.action.setBadgeText({ tabId: tab.id, text: "!" });
    await chrome.action.setTitle({ tabId: tab.id, title: `Filee: ${error.message}` });
  }
});
