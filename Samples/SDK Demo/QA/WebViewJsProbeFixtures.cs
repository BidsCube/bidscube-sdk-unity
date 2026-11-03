namespace BidscubeSDK.QA
{
    /// <summary>
    /// Hardcoded banner HTML creatives for WebView location/referrer QA.
    /// Ported from Flutter <c>webview_js_probe_fixtures.dart</c>.
    /// </summary>
    public static class WebViewJsProbeFixtures
    {
        const string ImgSimpleDataUri =
            "data:image/svg+xml;charset=utf-8," +
            "%3Csvg%20xmlns%3D%27http%3A//www.w3.org/2000/svg%27%20width%3D%27320%27%20height%3D%2750%27%3E" +
            "%3Crect%20width%3D%27100%25%27%20height%3D%27100%25%27%20fill%3D%27%23111%27/%3E" +
            "%3Ctext%20x%3D%27160%27%20y%3D%2732%27%20fill%3D%27%23fff%27%20font-size%3D%2714%27%20" +
            "text-anchor%3D%27middle%27%20font-family%3D%27sans-serif%27%3ETest%20Banner%20320x50%3C/text%3E" +
            "%3C/svg%3E";

        const string ImgDspDataUri =
            "data:image/svg+xml;charset=utf-8," +
            "%3Csvg%20xmlns%3D%27http%3A//www.w3.org/2000/svg%27%20width%3D%27320%27%20height%3D%2750%27%3E" +
            "%3Crect%20width%3D%27100%25%27%20height%3D%27100%25%27%20fill%3D%27%23c00%27/%3E" +
            "%3Ctext%20x%3D%27160%27%20y%3D%2732%27%20fill%3D%27%23fff%27%20font-size%3D%2714%27%20" +
            "text-anchor%3D%27middle%27%20font-family%3D%27sans-serif%27%3EDSP%20wrapper%20banner%3C/text%3E" +
            "%3C/svg%3E";

        const string Pixel1x1DataUri =
            "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7";

        public static string Simple320x50Html =>
            "<html><head><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"></head>" +
            "<body style=\"margin:0;padding:0;overflow:hidden;\">" +
            "<a href=\"https://example.com\"><img src=\"" + ImgSimpleDataUri +
            "\" width=\"320\" height=\"50\" style=\"display:block;width:320px;height:50px;border:0;\" alt=\"Test Banner\"></a>" +
            "</body></html>";

        /// <summary>DSP wrapper with hostname check pixel (primary banner probe fixture).</summary>
        public static string DspWrapperWithLocationCheckHtml =>
            "<div id=\"wrapper_f5a3165ae94c7d5d5daad47cbe4ce42c\" " +
            "style=\"margin:0;padding:0;min-height:50px;background:#222;overflow:hidden;\">" +
            "<a href=\"https://www.google.com\" target=\"_blank\" style=\"display:block;\">" +
            "<img id=\"dsp_main_img\" src=\"" + ImgDspDataUri + "\" " +
            "data-original-src=\"https://ftp.da-dsp.com/11060-82630d4c5e3cefcfb41960904b057cb1.jpeg\" " +
            "alt=\"dsp\" style=\"display:block;width:100%;height:auto;min-height:50px;background:#444;\">" +
            "</a>" +
            "<script type=\"application/javascript\"></script>" +
            "<img src=\"" + Pixel1x1DataUri + "\" width=\"1\" height=\"1\" alt=\"\">" +
            "</div>" +
            "<script type=\"application/javascript\">" +
            "(function () {" +
            "  var hostname = window.location.hostname;" +
            "  var checkUrl = 'https://da-dsp.com/?t=check&uniq=f5a3165ae94c7d5d5daad47cbe4ce42c&u=' + encodeURIComponent(hostname);" +
            "  var newImg = document.createElement('img');" +
            "  newImg.setAttribute('src', '" + Pixel1x1DataUri + "');" +
            "  newImg.setAttribute('data-check-url', checkUrl);" +
            "  newImg.setAttribute('alt', '');" +
            "  newImg.setAttribute('height', '1');" +
            "  newImg.setAttribute('width', '1');" +
            "  var wrap = document.getElementById('wrapper_f5a3165ae94c7d5d5daad47cbe4ce42c');" +
            "  if (wrap) { wrap.appendChild(newImg); }" +
            "  console.log('[BidscubeProbe] dsp check hostname=' + hostname);" +
            "  console.log('[BidscubeProbe] dsp checkPixel=' + checkUrl);" +
            "})();" +
            "</script>";
    }
}
