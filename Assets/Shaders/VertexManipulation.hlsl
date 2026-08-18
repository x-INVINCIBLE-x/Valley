#ifndef VERTEX_MANIPULATION_VERTEX_INCLUDED
#define VERTEX_MANIPULATION_VERTEX_INCLUDED


float3 TwistArountAxis(float3 pos , float3 axis , float amount)
{   
    if (abs(amount) < 0.0001)
        return pos;

	float height = dot(pos,axis);
	float angle = height * amount;

	float s,c;
	sincos(angle,s,c);

	float3 radical = pos - axis * height;

	return axis * height + radical * c + cross(axis , radical) * s;
}

float3 BendAroundAxis(float3 pos,float3 bendAxis,float3 bendDirection,float bendAmount)
{   

    if (abs(bendAmount) < 0.0001)
        return pos;

    bendAxis = normalize(bendAxis);

    // Ensure bendDirection is perpendicular to bendAxis
    bendDirection = normalize(bendDirection - bendAxis * dot(bendDirection, bendAxis));
        
    float height = dot(pos, bendAxis);

    float3 radial = pos - bendAxis * height;
        

    float radius = 1.0 / (bendAmount);

    float angle = height * bendAmount;

    float s, c;
    sincos(angle, s, c);

    float3 bentCenter = bendDirection * ((1.0 - c) * radius) + bendAxis * (s * radius);
        
    return bentCenter + radial;
}

#endif