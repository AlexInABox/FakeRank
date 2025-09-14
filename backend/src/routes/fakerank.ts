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

		// Extract user ID from URL parameters
		const url = new URL(request.url);
		const userId = url.searchParams.get('userid');

		if (!userId) {
			return new Response('User ID is required', { status: 400 });
		}

		// Query the playerdata table for the user's fakerank, fakerank_color, and fakerank_until
		const stmt = env.DB.prepare('SELECT fakerank, fakerank_color, fakerank_until FROM playerdata WHERE id = ?');
		const result = await stmt.bind(userId).first();

		if (!result) {
			return new Response('Player data not found', { status: 404 });
		}

		// Extract fakerank, fakerank_color, and fakerank_until from the result
		const fakerank = result.fakerank;
		const fakerankColor = result.fakerank_color;
		const fakerankUntil = result.fakerank_until;

		// Check if fakerank exists
		if (fakerank === null || fakerank === undefined || fakerank === '') {
			return new Response('Fakerank not found', { status: 404 });
		}

		// Check if fakerank is still valid (timestamp must be in the future)
		const currentTimestamp = Math.floor(Date.now() / 1000); // Current Unix timestamp
		if (typeof fakerankUntil !== 'number' || fakerankUntil <= currentTimestamp) {
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
	} catch (error) {
		console.error('FakeRank GET error:', error);
		return new Response('Server error', { status: 500 });
	}
}
