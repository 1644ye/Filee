(() => {
  "use strict";
  if (globalThis.__fileeSvgDrag) return;
  globalThis.__fileeSvgDrag = true;
  const ns = "http://www.w3.org/2000/svg";
  const properties = ["color", "fill", "fill-opacity", "fill-rule", "stroke", "stroke-width",
    "stroke-opacity", "stroke-linecap", "stroke-linejoin", "stroke-miterlimit", "stroke-dasharray",
    "stroke-dashoffset", "opacity", "visibility", "display", "clip-path", "clip-rule", "mask",
    "filter", "paint-order", "vector-effect", "font-family", "font-size", "font-style", "font-weight",
    "text-anchor", "dominant-baseline", "letter-spacing", "word-spacing", "stop-color", "stop-opacity"];
  let overlay, original, prepared, imageUrl, pressed = false, dragging = false, pointer;

  function pictureAddress(image) {
    // Use only URLs the page actually supplies. Keep query strings, signatures and format parameters intact.
    const candidates = [];
    for (const entry of (image.getAttribute("srcset") || "").split(",")) {
      const match = entry.trim().match(/^(\S+)\s+(\d+(?:\.\d+)?)(w|x)$/);
      if (match) candidates.push({ url: match[1], score: Number(match[2]) * (match[3] === "x" ? image.clientWidth : 1) });
    }
    const current = image.currentSrc || image.src;
    candidates.push({ url: current, score: image.naturalWidth || 0 });
    for (const candidate of candidates.sort((a, b) => b.score - a.score)) {
      const url = new URL(candidate.url, image.baseURI);
      if (/^https?:$/.test(url.protocol) || url.protocol === "data:" && /^data:image\//i.test(url.href)) return url.href;
    }
    throw new Error("This picture does not expose a downloadable image URL.");
  }

  function snapshot(svg) {
    const copy = svg.cloneNode(true);
    const pending = [];
    const ids = new Set([...copy.querySelectorAll("[id]")].map(e => e.id));
    if (copy.id) ids.add(copy.id);
    let references = 0;
    function localReference(value) {
      if (value.startsWith("#")) return value;
      const url = new URL(value, svg.baseURI);
      const page = new URL(svg.ownerDocument.URL);
      if (url.origin === page.origin && url.pathname === page.pathname && url.search === page.search && url.hash)
        return url.hash;
      throw new Error("This SVG needs an external resource; direct drag cannot preserve it yet.");
    }
    function reference(value) {
      const fragment = localReference(value);
      pending.push(decodeURIComponent(fragment.slice(1)));
      return fragment;
    }
    function urls(value) {
      return value.replace(/url\(\s*["']?([^"')]+)["']?\s*\)/g, (_, url) => `url("${reference(url.trim())}")`);
    }
    function prepare(source, target, root = false) {
      const originals = [source, ...source.querySelectorAll("*")];
      const clones = [target, ...target.querySelectorAll("*")];
      if (originals.length > 20000) throw new Error("SVG is too large to drag.");
      originals.forEach((element, i) => {
        const clone = clones[i];
        if (["script", "style", "foreignObject", "animate", "animateTransform", "set"].includes(element.localName)) {
          clone.remove(); return;
        }
        for (const attribute of [...clone.attributes]) {
          if (/^on/i.test(attribute.name)) clone.removeAttributeNode(attribute);
          else if (attribute.localName === "href") clone.setAttributeNS(attribute.namespaceURI, attribute.name, reference(attribute.value));
          else if (attribute.value.includes("url(")) clone.setAttribute(attribute.name, urls(attribute.value));
        }
        const style = getComputedStyle(element);
        clone.removeAttribute("class");
        clone.removeAttribute("style");
        for (const property of properties) {
          const value = style.getPropertyValue(property);
          if (value) clone.style.setProperty(property, urls(value));
        }
        if (!(root && i === 0) && style.transform !== "none") clone.style.transform = style.transform;
      });
    }
    prepare(svg, copy, true);
    const defs = document.createElementNS(ns, "defs");
    while (pending.length) {
      if (++references > 512) throw new Error("Too many SVG references.");
      const id = pending.shift();
      if (ids.has(id)) continue;
      const source = svg.ownerDocument.getElementById(id);
      if (!source || source.namespaceURI !== ns) throw new Error(`Missing SVG definition: ${id}`);
      const cloned = source.cloneNode(true);
      ids.add(id);
      for (const child of cloned.querySelectorAll("[id]")) ids.add(child.id);
      prepare(source, cloned);
      defs.append(cloned);
    }
    if (defs.childNodes.length) copy.prepend(defs);
    copy.setAttribute("xmlns", ns);
    const rect = svg.getBoundingClientRect();
    copy.setAttribute("width", String(svg.width.baseVal.value || rect.width));
    copy.setAttribute("height", String(svg.height.baseVal.value || rect.height));
    copy.style.display = "block";
    const result = new XMLSerializer().serializeToString(copy);
    if (result.length > 4 * 1024 * 1024) throw new Error("SVG is too large to drag.");
    return result;
  }

  function clear() {
    overlay?.remove(); overlay = original = prepared = imageUrl = null; pressed = dragging = false;
  }
  function arm(x, y) {
    if (dragging || pressed) return;
    if (overlay) overlay.style.pointerEvents = "none";
    const hit = document.elementFromPoint(x, y);
    if (overlay) overlay.style.pointerEvents = "auto";
    let svg = hit?.closest("svg");
    while (svg?.parentElement?.closest("svg")) svg = svg.parentElement.closest("svg");
    let picture = hit?.closest("img");
    if (!svg && !picture) {
      const card = hit?.closest("a");
      const images = card?.querySelectorAll("img");
      if (images?.length === 1) {
        const bounds = images[0].getBoundingClientRect();
        if (x >= bounds.left && x <= bounds.right && y >= bounds.top && y <= bounds.bottom) picture = images[0];
      }
    }
    const target = svg || picture;
    if (target === original) return;
    clear();
    if (!target) return;
    const box = target.getBoundingClientRect();
    if (box.width <= 0 || box.height <= 0) return;
    try {
      if (svg) prepared = snapshot(svg);
      else imageUrl = pictureAddress(picture);
    }
    catch (error) { console.warn("Filee SVG drag:", error.message); return; }
    original = target;
    const image = document.createElement("img");
    image.alt = "";
    image.setAttribute("aria-hidden", "true");
    image.draggable = true;
    image.src = imageUrl || "data:image/svg+xml;charset=utf-8," + encodeURIComponent(prepared);
    image.title = "Ctrl + drag to Filee";
    image.style.cssText = `position:fixed!important;left:${box.left}px!important;top:${box.top}px!important;` +
      `width:${box.width}px!important;height:${box.height}px!important;margin:0!important;padding:0!important;` +
      "border:0!important;max-width:none!important;max-height:none!important;z-index:2147483647!important;" +
      "opacity:0.01!important;cursor:grab!important;pointer-events:auto!important;user-select:none!important;";
    image.addEventListener("mousedown", event => { pressed = event.ctrlKey; });
    image.addEventListener("dragstart", event => {
      if (!event.ctrlKey || !(prepared || imageUrl) || !event.dataTransfer) { event.preventDefault(); clear(); return; }
      dragging = true;
      event.stopImmediatePropagation();
      event.dataTransfer.clearData();
      event.dataTransfer.effectAllowed = "copy";
      if (prepared) {
        event.dataTransfer.setData("image/svg+xml", prepared);
        event.dataTransfer.setData("text/plain", prepared);
        event.dataTransfer.setData("text/html", prepared);
        event.dataTransfer.setData("DownloadURL", "image/svg+xml:image.svg:" + image.src);
      } else {
        const standalone = document.createElement("img");
        standalone.src = imageUrl;
        event.dataTransfer.setData("text/html", standalone.outerHTML);
        event.dataTransfer.setData("text/uri-list", imageUrl);
        event.dataTransfer.setData("text/plain", imageUrl);
      }
      event.dataTransfer.setDragImage(original, 0, 0);
    });
    image.addEventListener("dragend", clear);
    overlay = image;
    document.documentElement.append(image);
  }
  document.addEventListener("mousemove", event => {
    pointer = [event.clientX, event.clientY];
    if (event.ctrlKey && !event.buttons) arm(...pointer);
    else if (!event.ctrlKey && !dragging) clear();
  }, true);
  document.addEventListener("keydown", event => { if (event.key === "Control" && pointer) arm(...pointer); }, true);
  document.addEventListener("keyup", event => { if (event.key === "Control" && !dragging) clear(); }, true);
  document.addEventListener("mouseup", () => { if (!dragging) clear(); }, true);
  window.addEventListener("blur", () => { if (!dragging) clear(); });
  window.addEventListener("scroll", () => { if (!dragging) clear(); }, true);
  window.addEventListener("resize", () => { if (!dragging) clear(); });
})();
