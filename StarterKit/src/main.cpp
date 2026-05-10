// Lesson 2: blink without delay
#include <Arduino.h>

const int ledPin = 13;
unsigned long previousMillis = 0;
const long interval = 500;
bool ledState = LOW;

void setup() {
  pinMode(ledPin, OUTPUT);
  Serial.begin(115200);
}

void loop() {
  unsigned long currentMillis = millis();

  if (currentMillis - previousMillis >= interval) {
    previousMillis = currentMillis;
    ledState = !ledState;
    digitalWrite(ledPin, ledState);
    Serial.println(ledState ? "LED on" : "LED off");
  }
}
