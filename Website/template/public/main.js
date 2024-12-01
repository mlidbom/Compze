export default {
    start: () => {
        removeRegionMarkerLines();
    },
}

function removeRegionMarkerLines() {
    const codeBlocks = document.querySelectorAll('pre code');
    codeBlocks.forEach(block => {
        block.innerHTML = block.innerHTML.replace(/^\s*#(region|endregion).*\n/gm, '');
    });
}