export async function onRequestGet(request: Request, env: Env, ctx: ExecutionContext): Promise<Response> {
	try {
		const authHeader = request.headers.get('Authorization');

		console.log('FakeRank GET request received with auth header:', authHeader);
		console.log('Must match token:', env.APITOKEN);

		// Verify Bearer token against environment variable
		if (!authHeader || !authHeader.startsWith('Bearer ')) {
			return new Response('Unauthorized', { status: 401 });
		}

		const token = authHeader.slice(7); // Remove 'Bearer ' prefix
		if (token !== env.APITOKEN) {
			return new Response('Unauthorized', { status: 401 });
		}

		// Extract user IDs from URL parameters (can be single or multiple)
		const url = new URL(request.url);
		const userIds = url.searchParams.getAll('userid');

		if (!userIds || userIds.length === 0) {
			return new Response('At least one User ID is required', { status: 400 });
		}

		// Handle single user ID request (backward compatibility)
		if (userIds.length === 1) {
			const userId = userIds[0];

			// Query the playerdata table for the user's fakerank, fakerank_color, fakerank_until, and fakerankoverride_until
			const stmt = env.DB.prepare('SELECT fakerank, fakerank_color, fakerank_until, fakerankoverride_until FROM playerdata WHERE id = ?');
			const result = await stmt.bind(userId).first();

			if (!result) {
				return new Response('Player data not found', { status: 404 });
			}

			// Extract fakerank, fakerank_color, fakerank_until, and fakerankoverride_until from the result
			const fakerank = result.fakerank;
			const fakerankColor = result.fakerank_color;
			const fakerankUntil = result.fakerank_until;
			const fakerankOverrideUntil = result.fakerankoverride_until;

			// Check if fakerank exists
			if (fakerank === null || fakerank === undefined || fakerank === '') {
				return new Response('Fakerank not found', { status: 404 });
			}

			// Check if fakerank is still valid
			const currentTimestamp = Math.floor(Date.now() / 1000); // Current Unix timestamp

			// Check if override is active (takes precedence)
			const hasOverride = typeof fakerankOverrideUntil === 'number' && fakerankOverrideUntil > currentTimestamp;

			// If no override, check regular fakerank_until timestamp
			if (!hasOverride && (typeof fakerankUntil !== 'number' || fakerankUntil <= currentTimestamp)) {
				return new Response('Fakerank expired', { status: 403 });
			}

			// Return both fakerank and fakerank_color as a tuple (comma-separated)
			const response = `${fakerank},${fakerankColor}`;

			return new Response(response, {
				status: 200,
				headers: {
					'Content-Type': 'text/plain',
				},
			});
		}

		// Handle multiple user IDs request
		const currentTimestamp = Math.floor(Date.now() / 1000);
		const results = [];

		// Create a query with multiple placeholders for the IN clause
		const placeholders = userIds.map(() => '?').join(',');
		const stmt = env.DB.prepare(
			`SELECT id, fakerank, fakerank_color, fakerank_until, fakerankoverride_until FROM playerdata WHERE id IN (${placeholders})`
		);
		const dbResults = await stmt.bind(...userIds).all();

		if (!dbResults.results || dbResults.results.length === 0) {
			return new Response('No player data found', { status: 404 });
		}

		// Process each result
		for (const result of dbResults.results) {
			const fakerank = result.fakerank;
			const fakerankColor = result.fakerank_color;
			const fakerankUntil = result.fakerank_until;
			const fakerankOverrideUntil = result.fakerankoverride_until;
			const userId = result.id;

			// Skip if no fakerank
			if (fakerank === null || fakerank === undefined || fakerank === '') {
				continue;
			}

			// Check if fakerank is still valid
			const hasOverride = typeof fakerankOverrideUntil === 'number' && fakerankOverrideUntil > currentTimestamp;

			// If no override, check regular fakerank_until timestamp
			if (!hasOverride && (typeof fakerankUntil !== 'number' || fakerankUntil <= currentTimestamp)) {
				continue; // Skip expired fakeranks
			}

			// Add to results: userid,fakerank,color
			results.push(`${userId},${fakerank},${fakerankColor}`);
		}

		// Return results separated by semicolons (as expected by C# code)
		const response = results.join(';');

		return new Response(response, {
			status: 200,
			headers: {
				'Content-Type': 'text/plain',
			},
		});
	} catch (error) {
		console.error('FakeRank GET error:', error);
		return new Response('Server error', { status: 500 });
	}
}
