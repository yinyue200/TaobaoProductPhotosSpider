// ==UserScript==
// @name        tmallreviewreciver
// @namespace   yinyue200.tmallreviewreciver
// @match       https://detail.tmall.com/item.htm*
// @grant       GM_xmlhttpRequest
// @connect     *
// @version     1.0
// @author      -
// @description 8/19/2021, 6:32:37 PM
// ==/UserScript==


yinyue200_tmallreviewobserver = null;
yinyue200startcheck_callback = function () {
    if (yinyue200_tmallreviewobserver != null) {
        yinyue200_tmallreviewobserver.disconnect();
    }

    // Select the node that will be observed for mutations
    const targetNode = document.querySelector('.rate-grid');
    if (targetNode == null) {
        return;
    }

    // Options for the observer (which mutations to observe)
    const config = { attributes: true, childList: true, subtree: true };

    // Callback function to execute when mutations are observed
    const callback = function (mutationsList, observer) {
        GM_xmlhttpRequest({
            method: "post",
            url: "http://localhost:16832/yinyue200/TaobaoInfoReciever/postinfo",
            data: JSON.stringify({ "url": window.location.href, "fullhtml": window.document.documentElement.outerHTML }),
            overrideMimeType: "application/json",
            onload: function (response) {
                console.log("yinyue200_tmallreviewobserver:  AJAX ONLOAD!")
            },
            onerror: function (response) {
                console.log("yinyue200_tmallreviewobserver:  AJAX ERROR!")
            }
        });
        console.log('yinyue200_tmallreviewobserver: load a new reviews page');
    };

    // Create an observer instance linked to the callback function
    yinyue200_tmallreviewobserver = new MutationObserver(callback);

    // Start observing the target node for configured mutations
    yinyue200_tmallreviewobserver.observe(targetNode, config);
    window.clearInterval(yinyue200startcheck_internal);

};
yinyue200startcheck_internal = window.setInterval(yinyue200startcheck_callback, 2000);