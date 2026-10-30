// Static content scripts cover all normal websites. Remove the previous version's per-site registrations.
chrome.runtime.onInstalled.addListener(async () => {
  const registered = await chrome.scripting.getRegisteredContentScripts();
  const ids = registered.filter(script => script.id.startsWith("filee-")).map(script => script.id);
  if (ids.length) await chrome.scripting.unregisterContentScripts({ ids });
});

// Convenient activation for a tab that was already open when the extension was installed or reloaded.
chrome.action.onClicked.addListener(async tab => {
  try {
    const url = new URL(tab.url);
    if (!/^https?:$/.test(url.protocol)) throw new Error("Open a normal website first.");
    await chrome.scripting.executeScript({ target: { tabId: tab.id }, files: ["svg-drag.js"] });
    await chrome.action.setBadgeText({ tabId: tab.id, text: "ON" });
    await chrome.action.setTitle({ tabId: tab.id, title: "Hold Ctrl and drag an SVG picture to Filee" });
  } catch (error) {
    await chrome.action.setBadgeText({ tabId: tab.id, text: "!" });
    await chrome.action.setTitle({ tabId: tab.id, title: `Filee: ${error.message}` });
  }
});
