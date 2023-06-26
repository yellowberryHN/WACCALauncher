#include "raylib.h"

bool prefix(const char* pre, const char* str)
{
    return strncmp(pre, str, strlen(pre)) == 0;
}

typedef enum {
    TEST_BUTTON = GAMEPAD_BUTTON_RIGHT_FACE_DOWN,
    SERVICE_BUTTON = GAMEPAD_BUTTON_RIGHT_FACE_RIGHT,
    VOLUME_UP = GAMEPAD_BUTTON_LEFT_FACE_UP,
    VOLUME_DOWN = GAMEPAD_BUTTON_LEFT_FACE_DOWN
} IOButtons;

int main(void)
{
    SetTraceLogLevel(LOG_ALL);

    const int windowSize = 1080;
    int monitorIndex = 0;
    bool ioBoard = false;

    InitWindow(windowSize, windowSize, "WACCA Launcher");

    for (int i = 0; i < GetMonitorCount(); i++) {
        Vector2 size = { GetMonitorWidth(i), GetMonitorHeight(i) };
        if (size.x < size.y) { // vertical monitor that is at least
            monitorIndex = i;
            TraceLog(LOG_DEBUG, "Monitor %i passed check", i);
        }
        else TraceLog(LOG_DEBUG, "Monitor %i failed check", i);
    }

    // TODO: for some reason this doesn't work
    SetGamepadMappings("0300bd19a30c00002100000000000000,I/O CONTROL BD;15257 ;01;91;3EEE;6679B;00;GOUT=14_ADIN=8,E_ROTIN=4_COININ=2_SWIN=2,E_UQ1=41,6;,a:b9,b:b6,dpup:b1,dpdown:b0,platform:Windows,");
    
    Vector2 screen = GetMonitorPosition(monitorIndex);
    SetWindowPosition(screen.x, screen.y + 362); // TODO: hardcoded offset won't work on non-1080p displays.
    SetWindowState(FLAG_WINDOW_UNDECORATED);

    Font fontTtf = LoadFontEx("res/funny.ttf", 30, 0, 250);

    SetTargetFPS(60);

    int cursor = 0;

    // Main loop
    while (!WindowShouldClose())    // Detect window close button or ESC key
    {
        // Update
        //----------------------------------------------------------------------------------
        if (IsGamepadAvailable(0)) {
#if _DEBUG
            ioBoard = prefix("Logitech", GetGamepadName(0));
#else
            ioBoard = prefix("I/O CONTROL BD", GetGamepadName(0));
#endif

            if (IsGamepadButtonPressed(0, VOLUME_UP)) cursor++;
            if (IsGamepadButtonPressed(0, VOLUME_DOWN)) cursor--;
        }
        else ioBoard = false;

        //----------------------------------------------------------------------------------

        // Draw
        //----------------------------------------------------------------------------------
        BeginDrawing();

        ClearBackground(BLACK);

        Vector2 textBounds = MeasureTextEx(fontTtf, "LAUNCHER SETTINGS", (float)fontTtf.baseSize, 0.1f);
        DrawTextEx(fontTtf, "LAUNCHER SETTINGS", (Vector2) { 540 - (textBounds.x / 2), 126 }, (float)fontTtf.baseSize, 0.1f, WHITE);
        DrawText(TextFormat("Monitor position: %03i, %03i (%i)", screen.x, screen.y, GetMonitorCount()), 10, 40, 20, LIGHTGRAY);
        DrawText(TextFormat("IO Board detected: %c", ioBoard ? 'Y' : 'N'), 10, 540, 20, GREEN);
        DrawText(TextFormat("Buttons Pressed: %s, %s, %s, %s (%d)",
            IsGamepadButtonDown(0, TEST_BUTTON) ? "TEST" : "test",
            IsGamepadButtonDown(0, SERVICE_BUTTON) ? "SERVICE" : "service",
            IsGamepadButtonDown(0, VOLUME_UP) ? "UP" : "up",
            IsGamepadButtonDown(0, VOLUME_DOWN) ? "DOWN" : "down",
            cursor
        ), 10, 570, 20, ORANGE);
        

        if (IsGamepadAvailable(0)) {
            if (GetGamepadButtonPressed() != GAMEPAD_BUTTON_UNKNOWN) DrawText(TextFormat("DETECTED BUTTON: %i", GetGamepadButtonPressed()), 100, 500, 20, RED);
            else DrawText("DETECTED BUTTON: NONE", 100, 500, 20, GRAY);
        }

        EndDrawing();
        //----------------------------------------------------------------------------------
    }

    // De-Initialization
    //--------------------------------------------------------------------------------------

    UnloadFont(fontTtf);

    CloseWindow();        // Close window and OpenGL context
    //--------------------------------------------------------------------------------------

    return 0;
}