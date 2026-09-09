# App.cs

- SceneReferences: A component holding necessary references to things in the main scene (keeps us from having to use `FindObjectOfType`)
- Prefabs: A ScriptableObject used to reference prefabs used in the application (keeps us from having to use `Resources`)
- UI Primitives: A ScriptableObject used to reference reused UI elements like buttons, text, layouts, etc
- UI Elements: Identical to UI Primitives, but specific to Make It Sing

## Env

The first few settings are required fro the app to function. Everything under `Editor Overrides` is just there to make your life easier and is not required. Everything in this section is _unique to your development device_. It is not propagated to other developers.

- Supabase Project ID: Use the value I sent
- Supabase API Key: Use the value I sent
- Run In Offline Mode: When checked, the application won't attempt to connect to the backend, meaning you won't have to log in. You will also not be able to connect to other users while this is checked.
- Override Platform: When this is checked, the application will behave as if it is running on the platform set in `Platform`
- Platform: This sets what platform to override with, ignoring the default. **This does not change the build target, per-platform settings like graphics and shaders will not change**
- Override Config: When checked, the application will use the settings below instead of ones we pull from the server. This is useful for configuring your editor to make testing easy.
- Log Groups: What groups you want to see logs from. Typically this will be set to `Everything`.
- Log Level: What level of logs you want to see. Commonly this is set to `Error` (to filter out everything that isn't an error), or `Trace` (to see everything)
- Stack Trace Level: Don't worry about this- just set it to `Error`
- Notification Level: What level of notifications or "toasts" (little in-app popups) you want to see. Leaving this on `Info` will let you know when you've gained/lost connection to server.
- Login Automatically: When checked, the applications will log in automatically using the settings below
- Domain: The domain used to log in
- Username: The username used to log in
- Password: The password used to log in
- Disable System UI: When checked, all menus will be hidden. This is for hiding menus for demo participants, preventing them from messing things up.

> NOTE: You should not have to "log in" during the initial phase of testing. Logging in only matters if you're trying to use the VPS, which should only really ever happen on device.
