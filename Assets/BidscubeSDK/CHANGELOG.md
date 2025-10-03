# Changelog

All notable changes to the BidscubeSDK Unity project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.1] - 2024-12-19

### Added

#### Core SDK Features

- **Initial SDK Implementation**: Complete Unity SDK with cross-platform support
- **Ad Types Support**: Image, Video, Skippable Video, and Native ads
- **Ad Positioning System**: Full support for all ad positions (Header, Footer, Sidebar, FullScreen, etc.)
- **Error Handling**: Comprehensive error handling with specific error codes and messages
- **Logging System**: Debug logging with configurable levels

#### WebView Integration

- **WebView Support**: Integration with gree/unity-webview plugin for HTML ad rendering
- **HTML Content Rendering**: Support for HTML-based ads with proper structure wrapping
- **WebView Controllers**:
  - `NewWebViewController`: Simple WebView controller for HTML content
  - `WebViewController`: Advanced WebView controller with full feature set
- **Runtime HTML Loading**: Dynamic HTML content loading and display
- **WebView Positioning**: Automatic positioning based on ad placement

#### Ad Management

- **SDK Initialization**: Configurable SDK setup with `SDKConfig`
- **Ad Callback System**: Complete `IAdCallback` interface implementation
- **Ad Loading**: Asynchronous ad loading with progress tracking
- **Ad Display**: Multiple display methods (fullscreen, embedded, windowed)
- **Ad Interaction**: Click handling and browser redirection

#### Scene Management

- **Test Scenes**: Complete set of test scenes for different ad types
  - `SDKTestScene`: Main testing scene with all ad types
  - `ConsentTestScene`: GDPR/CCPA consent flow testing
  - `WindowedAdTestScene`: Windowed ad display testing
  - `BidscubeExampleScene`: Example implementation scene
- **Scene Setup Scripts**: Automated scene configuration helpers
- **Quick Setup**: Context menu options for rapid testing

#### Documentation

- **API Reference**: Complete API documentation with examples
- **Installation Guide**: Step-by-step installation instructions
- **README**: Comprehensive overview and quick start guide
- **Code Examples**: Extensive usage examples and best practices

#### Networking

- **URL Building**: Dynamic ad request URL construction
- **HTTP Requests**: UnityWebRequest-based ad content fetching
- **JSON Parsing**: Ad response parsing with error handling
- **Content Validation**: HTML content validation and structure checking

#### Error Handling & Debugging

- **Error Codes**: Categorized error codes (Network, Ad Loading, Display, Consent)
- **Debug Logging**: Comprehensive logging system without Unicode emojis
- **Error Recovery**: Graceful error handling and recovery mechanisms
- **Status Tracking**: Real-time ad loading and display status

#### HTML Content Processing

- **HTML Structure Detection**: Automatic detection of proper HTML structure
- **Content Wrapping**: Automatic wrapping of HTML fragments in complete documents
- **CSS Styling**: Responsive CSS for proper ad display
- **JavaScript Support**: Basic JavaScript execution in WebView

#### Platform Support

- **iOS Support**: Full iOS integration with proper WebView handling
- **Android Support**: Android WebView integration
- **Unity Editor**: Editor support for testing and development
- **Cross-Platform**: Unified API across all platforms

### Technical Implementation

#### Architecture

- **Modular Design**: Clean separation of concerns with dedicated controllers
- **Namespace Organization**: Proper namespace structure (`BidscubeSDK.Controllers`, `BidscubeSDK.Examples`)
- **Interface-Based**: Callback system using interfaces for flexibility
- **Async Operations**: Coroutine-based asynchronous operations

#### WebView Integration

- **Canvas Detection**: Automatic Canvas detection and assignment
- **Position Calculation**: Screen coordinate to RectTransform conversion
- **Margin Management**: Precise WebView positioning with margins
- **Visibility Control**: Show/hide WebView with proper state management

#### Content Management

- **HTML Validation**: Content structure validation and enhancement
- **Responsive Design**: Mobile-friendly HTML rendering
- **Link Handling**: Proper link click handling and browser redirection
- **Image Optimization**: Responsive image handling in HTML content

#### Performance

- **Memory Management**: Proper cleanup of WebView resources
- **Object Pooling**: Efficient GameObject management
- **Resource Cleanup**: Automatic resource disposal and cleanup
- **Error Recovery**: Robust error handling without crashes

### Configuration

#### SDK Configuration

```csharp
var config = new SDKConfig
{
    EnableLogging = true,
    EnableDebugMode = false,
    DefaultAdTimeout = 30000,
    DefaultAdPosition = AdPosition.Unknown
};
```

#### WebView Configuration

- **Margin Settings**: Configurable left, right, top, bottom margins
- **Visibility Control**: Show/hide WebView programmatically
- **Content Loading**: HTML content loading with validation
- **Error Handling**: WebView-specific error handling

### Examples

#### Basic Ad Display

```csharp
BidscubeSDK.ShowAd("19481", AdType.Image, callback);
```

#### WebView HTML Ads

```csharp
var webViewController = gameObject.AddComponent<NewWebViewController>();
webViewController.HTMLad = "<div>Your HTML content</div>";
```

#### Custom Positioning

```csharp
BidscubeSDK.SetAdPosition(AdPosition.Header);
```

### Dependencies

- **Unity**: 2020.3 or later
- **WebView Plugin**: gree/unity-webview
- **iOS**: 12.0 or later
- **Android**: API 21 or later

### Breaking Changes

None (initial release)

### Deprecated

None (initial release)

### Security

- **HTTPS Support**: Secure ad content loading
- **Content Validation**: HTML content sanitization
- **Error Handling**: Secure error message handling
- **Resource Management**: Proper resource cleanup

### Performance Improvements

- **Async Loading**: Non-blocking ad loading
- **Memory Efficiency**: Optimized WebView management
- **Resource Cleanup**: Automatic cleanup of unused resources
- **Error Recovery**: Graceful handling of network and display errors

### Bug Fixes

- **WebView Positioning**: Fixed Canvas assignment and positioning
- **HTML Rendering**: Fixed HTML content display issues
- **Error Handling**: Fixed callback parameter mismatches
- **Memory Leaks**: Fixed WebView resource cleanup
- **Unicode Issues**: Removed all Unicode emojis from log messages

### Testing

- **Unit Tests**: Basic functionality testing
- **Integration Tests**: WebView integration testing
- **Scene Tests**: Complete scene-based testing
- **Error Testing**: Error condition testing
- **Performance Testing**: Memory and performance validation

### Documentation

- **API Documentation**: Complete API reference
- **Installation Guide**: Step-by-step setup instructions
- **Code Examples**: Extensive usage examples
- **Troubleshooting**: Common issues and solutions
- **Best Practices**: Recommended implementation patterns

---

## Version History

- **0.0.1** (2024-12-19): Initial release with core SDK functionality, WebView integration, and comprehensive documentation
