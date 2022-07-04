#ifndef SPHERE_FUNC
#define SPHERE_FUNC

// From 0 (color2) to 1 (color1)
float4 interpolateColor(float level, float4 color1, float4 color2)
{
    if (level >= 1)
        return color1;
    if (level <= 0)
        return color2;
    float4 newColor = level * color1 + (1 - level) * color2;
    return newColor;
}

float4 overlandColor(float height, float waterLevel, float temperature, float humidity, float desertThreshold1, float desertThreshold2, float highHumidityLightnessPercentage, float4 desertColor,
    float colorStep1, float4 color1, float colorStep2, float4 color2, float colorStep3, float4 color3, float colorStep4, float4 color4, float colorStep5, float4 color5, float colorStep6, float4 color6, float colorStep7, float4 color7, float colorStep8, float4 color8)
{
    float4 landColor = float4(0, 0, 0, 0);
    float desertRate = 0;
    float desertTendency = temperature * (1 - humidity);
    if (desertTendency >= desertThreshold1 && desertTendency < desertThreshold2)
        desertRate = (desertTendency - desertThreshold1) / (desertThreshold2 - desertThreshold1);
    else if (desertTendency >= desertThreshold2)
        return desertColor;

    float newHeight = (height - waterLevel) / (1 - waterLevel);

    if (newHeight <= colorStep1)
        landColor = color1;
    else if (newHeight <= colorStep2)
        landColor = interpolateColor((newHeight - colorStep2) / (colorStep1 - colorStep2), color1, color2);
    else if (newHeight <= colorStep3)
        landColor = interpolateColor((newHeight - colorStep3) / (colorStep2 - colorStep3), color2, color3);
    else if (newHeight <= colorStep4)
        landColor = interpolateColor((newHeight - colorStep4) / (colorStep3 - colorStep4), color3, color4);
    else if (newHeight <= colorStep5)
        landColor = interpolateColor((newHeight - colorStep5) / (colorStep4 - colorStep5), color4, color5);
    else if (newHeight <= colorStep6)
        landColor = interpolateColor((newHeight - colorStep6) / (colorStep5 - colorStep6), color5, color6);
    else if (newHeight <= colorStep7)
        landColor = interpolateColor((newHeight - colorStep7) / (colorStep6 - colorStep7), color6, color7);
    else if (newHeight <= colorStep8)
        landColor = interpolateColor((newHeight - colorStep8) / (colorStep7 - colorStep8), color7, color8);
    else landColor = color8;

    if (desertRate > 0)
    {
        float humidityLightness = (1 - highHumidityLightnessPercentage * (1 - humidity) * (1 - desertRate));
        landColor *= humidityLightness;
        landColor = interpolateColor(desertRate, desertColor, landColor);
    }
    else
    {
        float humidityLightness = (1 - highHumidityLightnessPercentage * (1 - humidity));
        landColor *= humidityLightness;
    }
    return landColor;
}

float4 oceanColor(float height, float waterLevel,
    float oceanColorStep1, float4 oceanColor1, float oceanColorStep2, float4 oceanColor2, float oceanColorStep3, float4 oceanColor3, float oceanColorStep4, float4 oceanColor4)
{
    float4 oceanColor = float4(0, 0, 0, 0);
    float newHeight = 1 - (height / waterLevel);

    if (newHeight <= oceanColorStep1)
        oceanColor = oceanColor1;
    else if (newHeight <= oceanColorStep2)
        oceanColor = interpolateColor((newHeight - oceanColorStep2) / (oceanColorStep1 - oceanColorStep2), oceanColor1, oceanColor2);
    else if (newHeight <= oceanColorStep3)
        oceanColor = interpolateColor((newHeight - oceanColorStep3) / (oceanColorStep2 - oceanColorStep3), oceanColor2, oceanColor3);
    else if (newHeight <= oceanColorStep4)
        oceanColor = interpolateColor((newHeight - oceanColorStep4) / (oceanColorStep3 - oceanColorStep4), oceanColor3, oceanColor4);
    else oceanColor = oceanColor4;
    return oceanColor;
}

float4 colorAtElevation(float height, float waterLevel, float temperature, float humidity,
                        float iceTemperatureThreshold1, float iceTemperatureThreshold2,
                        float desertThreshold1, float desertThreshold2,
                        float highHumidityLightnessPercentage, float4 iceColor, float4 desertColor,
                        int isLandMastSet, int isAboveWater,
                        float colorStep1, float4 color1, float colorStep2, float4 color2, float colorStep3, float4 color3, float colorStep4, float4 color4, float colorStep5, float4 color5, float colorStep6, float4 color6, float colorStep7, float4 color7, float colorStep8, float4 color8,
                        float oceanColorStep1, float4 oceanColor1, float oceanColorStep2, float4 oceanColor2, float oceanColorStep3, float4 oceanColor3, float oceanColorStep4, float4 oceanColor4)
{
    //if (temperature <= iceTemperatureThreshold2)
    //    return iceColor;

    float4 landColor = float4(0, 0, 0, 0);
    if (isLandMastSet > 0)
    {
        if (height > waterLevel)
        {
            landColor = overlandColor(height, waterLevel, temperature, humidity, desertThreshold1, desertThreshold2, highHumidityLightnessPercentage, desertColor,
                colorStep1, color1, colorStep2, color2, colorStep3, color3, colorStep4, color4, colorStep5, color5, colorStep6, color6, colorStep7, color7, colorStep8, color8);
        }
        else
        {
            landColor = oceanColor(height, waterLevel, oceanColorStep1, oceanColor1, oceanColorStep2, oceanColor2, oceanColorStep3, oceanColor3, oceanColorStep4, oceanColor4);
        }
    }
    else if (height > waterLevel)
    {
        landColor = overlandColor(height, waterLevel, temperature, humidity, desertThreshold1, desertThreshold2, highHumidityLightnessPercentage, desertColor,
            colorStep1, color1, colorStep2, color2, colorStep3, color3, colorStep4, color4, colorStep5, color5, colorStep6, color6, colorStep7, color7, colorStep8, color8);
    }
    else
    {
        landColor = oceanColor(height, waterLevel, oceanColorStep1, oceanColor1, oceanColorStep2, oceanColor2, oceanColorStep3, oceanColor3, oceanColorStep4, oceanColor4);
    }

    if (temperature < iceTemperatureThreshold1)
    {
        float temperatureRatio = (iceTemperatureThreshold1 - temperature) / (iceTemperatureThreshold1 - iceTemperatureThreshold2);
        landColor = (iceColor - landColor) * temperatureRatio + landColor;
        //landColor = interpolateColor(temperatureRatio, iceColor, landColor);
    }
    return landColor;
}

#define MIN_TEMPERATURE -15
#define MAX_TEMPERATURE 45

#define LIGHT_BLUE float4(0.5, 0.5, 1, 1)
#define DEEP_BLUE float4(0, 0, 1, 1)
#define CYAN float4(0, 1, 1, 1)
#define YELLOW float4(1, 1, 0, 1)
#define RED float4(1, 0, 0, 1)
#define DEEP_RED float4(0.5, 0, 0, 1)

#define DARK_BLUE float4(0, 0, 0.5, 1)
#define SAND float4(0.77, 1, 0.57, 1)

float4 temperatureColor(float temperature)
{
    if (temperature < -15)
        return LIGHT_BLUE;
    else if (temperature < -3)
        return interpolateColor((temperature + 15) / 12, DEEP_BLUE, LIGHT_BLUE);
    else if (temperature < 9)
        return interpolateColor((temperature + 3) / 12, CYAN, DEEP_BLUE);
    else if (temperature < 21)
        return interpolateColor((temperature - 9) / 12, YELLOW, CYAN);
    else if (temperature < 33)
        return interpolateColor((temperature - 21) / 12, RED, YELLOW);
    else if (temperature < 45)
        return interpolateColor((temperature - 33) / 12, DEEP_RED, RED);
    else
        return DEEP_RED;
}

float4 humidityColor(float humidity)
{
    if (humidity < 0)
        return SAND;
    else if (humidity < 0.25)
        return interpolateColor(humidity / 0.25, YELLOW, SAND);
    else if (humidity < 0.5)
        return interpolateColor((humidity - 0.25) / 0.25, LIGHT_BLUE, YELLOW);
    else if (humidity < 0.75)
        return interpolateColor((humidity - 0.5) / 0.25, DEEP_BLUE, LIGHT_BLUE);
    else if (humidity < 1)
        return interpolateColor((humidity - 0.75) / 0.25, DARK_BLUE, DEEP_BLUE);
    else
        return DARK_BLUE;
}
#endif
