mergeInto(LibraryManager.library, {
  StartAIBridge: function () {
    console.log("AIBridge.jslib: StartAIBridge function EXECUTED.");
    if (typeof window.startAIAssistant === 'function') {
      window.startAIAssistant();
      console.log("AIBridge: Successfully started AI Assistant.");
    } else {
      console.error("AIBridge Error: window.startAIAssistant() is not globally defined. Check lab-main.js and script loading order.");
    }
  },
});
