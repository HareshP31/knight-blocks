// --- Inside FirebaseJSHook.jslib ---

var FirebaseJSHook = {
  // ... (keep $db, $unityInstance, InitFirebase, SaveCreation the same) ...

  LoadCreations: function(unityObjectName) {
    if (!FirebaseJSHook.db) {
      console.error("Firestore DB is not initialized.");
      return;
    }

    // --- Use dynamic imports to get Firestore functions ---
    import('https://www.gstatic.com/firebasejs/10.12.2/firebase-firestore.js')
      .then(firestoreModule => {
          firestoreModule.getDocs(firestoreModule.collection(FirebaseJSHook.db, "creations"))
          .then(querySnapshot => {
            const creationsArray = [];
            querySnapshot.forEach(doc => {
              creationsArray.push(doc.data());
            });

            // *** THIS IS THE CHANGE ***
            // Wrap the array in an object for JsonUtility
            const resultObject = { items: creationsArray };
            // **************************

            // Send the WRAPPED object back as a JSON string
            const jsonResult = JSON.stringify(resultObject);

            FirebaseJSHook.unityInstance.SendMessage(
              Pointer_stringify(unityObjectName),
              "OnCreationsLoaded",
              jsonResult
            );
          })
          .catch(error => {
            console.error("Error loading documents: ", error);
            // Optionally send error back to Unity
            FirebaseJSHook.unityInstance.SendMessage(
                Pointer_stringify(unityObjectName),
                "OnFirebaseError",
                "Error loading creations: " + error.message
            );
          });
      })
      .catch(error => {
          console.error("Error importing Firestore module for loading:", error);
      });
  }
};

mergeInto(LibraryManager.library, FirebaseJSHook);