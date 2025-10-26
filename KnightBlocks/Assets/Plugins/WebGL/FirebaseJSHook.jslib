var FirebaseJSHook = {
  $db: null,
  $unityInstance: null,

  InitFirebase: function (configJson, unityObjectName) {
    FirebaseJSHook.unityInstance = window.unityInstance;

    const firebaseConfig = JSON.parse(UTF8ToString(configJson));
    const objectName = UTF8ToString(unityObjectName);

    try {
      const app = firebase.initializeApp(firebaseConfig);
      FirebaseJSHook.db = firebase.firestore();
      console.log("Firebase initialized!");

      FirebaseJSHook.unityInstance.SendMessage(objectName, "OnFirebaseInitialized");
    } catch (error) {
      console.error("Error initializing Firebase:", error);
      FirebaseJSHook.unityInstance.SendMessage(
        objectName,
        "OnFirebaseError",
        "Error initializing Firebase JS: " + error.message
      );
    }
  },

  SaveCreation: function (creationJson) {
    if (!FirebaseJSHook.db) {
      console.error("Firestore not initialized yet!");
      return;
    }

    const creation = JSON.parse(UTF8ToString(creationJson));
    firebase.firestore().collection("creations").add(creation)
      .then(docRef => console.log("Saved doc:", docRef.id))
      .catch(err => console.error("Save error:", err));
  },

  LoadCreations: function (unityObjectName) {
    const objectName = UTF8ToString(unityObjectName);
    if (!FirebaseJSHook.db) {
      FirebaseJSHook.unityInstance.SendMessage(objectName, "OnFirebaseError", "Firestore not ready");
      return;
    }

    firebase.firestore().collection("creations").get()
      .then(snapshot => {
        const creationsArray = [];
        snapshot.forEach(doc => creationsArray.push(doc.data()));
        const json = JSON.stringify({ items: creationsArray });
        FirebaseJSHook.unityInstance.SendMessage(objectName, "OnCreationsLoaded", json);
      })
      .catch(err => {
        console.error("Load error:", err);
        FirebaseJSHook.unityInstance.SendMessage(objectName, "OnFirebaseError", err.message);
      });
  }
};

mergeInto(LibraryManager.library, FirebaseJSHook);