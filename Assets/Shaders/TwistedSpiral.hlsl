#ifndef TWISTED_SPIRAL_VERTEX_INCLUDED
#define TWISTED_SPIRAL_VERTEX_INCLUDED

/////////////////////////////////////////////////////
// Rodrigues Rotation Formula
/////////////////////////////////////////////////////

float3 RotateAroundAxis(
    float3 position,
    float3 axis,
    float angle)
{
    axis = normalize(axis);

    float s = sin(angle);
    float c = cos(angle);

    return position * c +
           cross(axis, position) * s +
           axis * dot(axis, position) * (1.0 - c);
}

/////////////////////////////////////////////////////
// Rotate Normal Around Axis
/////////////////////////////////////////////////////

float3 RotateNormalAroundAxis(
    float3 normal,
    float3 axis,
    float angle)
{
    axis = normalize(axis);

    float s = sin(angle);
    float c = cos(angle);

    return normalize(
        normal * c +
        cross(axis, normal) * s +
        axis * dot(axis, normal) * (1.0 - c)
    );
}

/////////////////////////////////////////////////////
// GENERIC TWISTED SPIRAL
/////////////////////////////////////////////////////

float3 CurvedWorldTwistedSpiral(
    float3 positionOS,
    float3 rotationAxis,
    float twistStrength,
    float bendX,
    float bendY,
    float offset)
{
    rotationAxis = normalize(rotationAxis);

    //------------------------------------------------
    // Distance along axis
    //------------------------------------------------

    float axisDistance =
        dot(positionOS, rotationAxis);

    float d =
        max(0.0, axisDistance - offset);

    //------------------------------------------------
    // Twist
    //------------------------------------------------

    float angle =
        d * twistStrength;

    positionOS =
        RotateAroundAxis(
            positionOS,
            rotationAxis,
            angle);

    //------------------------------------------------
    // Build local basis
    //------------------------------------------------

    float3 helper =
        abs(rotationAxis.y) > 0.999
        ? float3(1,0,0)
        : float3(0,1,0);

    float3 tangent =
        normalize(
            cross(helper,
            rotationAxis));

    float3 bitangent =
        normalize(
            cross(rotationAxis,
            tangent));

    //------------------------------------------------
    // Bend
    //------------------------------------------------

    float bend =
        d * d;

    positionOS += tangent * bend * bendX;
    positionOS += bitangent * bend * bendY;

    return positionOS;
}

/////////////////////////////////////////////////////
// POSITION + NORMAL VERSION
/////////////////////////////////////////////////////

void CurvedWorldTwistedSpiralPositionAndNormal(
    inout float3 positionOS,
    inout float3 normalOS,
    float3 rotationAxis,
    float twistStrength,
    float bendX,
    float bendY,
    float offset)
{
    rotationAxis = normalize(rotationAxis);

    float axisDistance =
        dot(positionOS, rotationAxis);

    float d =
        max(0.0, axisDistance - offset);

    float angle =
        d * twistStrength;

    //------------------------------------------------
    // Twist Position
    //------------------------------------------------

    positionOS =
        RotateAroundAxis(
            positionOS,
            rotationAxis,
            angle);

    //------------------------------------------------
    // Twist Normal
    //------------------------------------------------

    normalOS =
        RotateNormalAroundAxis(
            normalOS,
            rotationAxis,
            angle);

    //------------------------------------------------
    // Basis
    //------------------------------------------------

    float3 helper =
        abs(rotationAxis.y) > 0.999
        ? float3(1,0,0)
        : float3(0,1,0);

    float3 tangent =
        normalize(
            cross(helper,
            rotationAxis));

    float3 bitangent =
        normalize(
            cross(rotationAxis,
            tangent));

    //------------------------------------------------
    // Bend
    //------------------------------------------------

    float bend =
        d * d;

    positionOS += tangent * bend * bendX;
    positionOS += bitangent * bend * bendY;
}

#endif