namespace TEST_DEF_03N
{
    internal static class ButtonTestRunner
    {
        internal static async Task<bool> WaitForButtonAsync(short expected,
            Func<CancellationToken, Task<short>> read, TimeSpan timeout,
            CancellationToken cancellationToken, int pollMs = 100)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(timeout);
            try
            {
                while (true)
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    try
                    {
                        short value = await read(deadline.Token).WaitAsync(deadline.Token);
                        deadline.Token.ThrowIfCancellationRequested();
                        if (value == expected) return true;
                    }
                    catch (TimeoutException) { }
                    catch (IOException) { }
                    catch (InvalidOperationException) { }
                    await Task.Delay(pollMs, deadline.Token);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }
    }
}
