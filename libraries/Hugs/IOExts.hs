-----------------------------------------------------------------------------
-- IO monad extensions:
--
-- Suitable for use with Hugs 98.
-----------------------------------------------------------------------------

module Hugs.IOExts
	( fixIO			-- :: (a -> IO a) -> IO a
	, unsafePerformIO	-- :: IO a -> a

	, performGC

	, IOModeEx(..)	      	-- instance (Eq, Read, Show)
	, openFileEx	      	-- :: FilePath -> IOModeEx -> IO Handle

	, unsafePtrEq
	, unsafePtrToInt
	, unsafeCoerce
	
	  -- backward compatibility with IOExtensions
	, readBinaryFile        -- :: FilePath -> IO String
	, writeBinaryFile       -- :: FilePath -> String -> IO ()
	, appendBinaryFile      -- :: FilePath -> String -> IO ()
	, openBinaryFile        -- :: FilePath -> IOMode -> IO Handle

	, hSetBinaryMode	-- :: Handle -> Bool -> IO ()
	, hPutBuf	        -- :: Handle -> Ptr a -> Int -> IO ()
	, hGetBuf	        -- :: Handle -> Ptr a -> Int -> IO Int

	, argv                  -- :: [String]

	-- Terminal operations
	, hIsTerminalDevice	-- :: Handle -> IO Bool
	, hGetEcho		-- :: Handle -> IO Bool
	, hSetEcho		-- :: Handle -> Bool -> IO ()

	  -- Non-standard extensions 
	, hugsIsEOF             -- :: IO Bool
	, hugsHIsEOF            -- :: Handle  -> IO Bool
	) where

import Hugs.Prelude
import Hugs.IO
import Hugs.IORef
import Hugs.System ( getArgs )
import Hugs.Ptr ( Ptr )

-----------------------------------------------------------------------------

primitive performGC "primGC" :: IO ()

unsafePerformIO :: IO a -> a
unsafePerformIO m = valueOf (basicIORun m)

primitive unsafePtrEq    :: a -> a -> Bool
primitive unsafePtrToInt :: a -> Int

fixIO :: (a -> IO a) -> IO a
fixIO f = do
	r <- newIORef (throw NonTermination)
	x <- f (unsafePerformIO (readIORef r))
	writeIORef r x
	return x

primitive unsafeCoerce "primUnsafeCoerce" :: a -> b

valueOf :: IOFinished a -> a
valueOf (Finished_Return a) = a
valueOf _ = error "IOExts.valueOf: thread failed"	-- shouldn't happen

-----------------------------------------------------------------------------
-- Binary files 
-----------------------------------------------------------------------------
data IOModeEx 
 = BinaryMode IOMode
 | TextMode   IOMode
   deriving (Eq, Read, Show)

openFileEx :: FilePath -> IOModeEx -> IO Handle
openFileEx fp m = 
  case m of
    BinaryMode m -> openBinaryFile fp m
    TextMode m   -> openFile fp m

argv :: [String]
argv = unsafePerformIO getArgs

primitive writeBinaryFile   	 :: FilePath -> String -> IO ()
primitive appendBinaryFile  	 :: FilePath -> String -> IO ()
primitive readBinaryFile    	 :: FilePath -> IO String
primitive openBinaryFile         :: FilePath -> IOMode -> IO Handle

primitive hSetBinaryMode	 :: Handle -> Bool -> IO ()
primitive hPutBuf	    	 :: Handle -> Ptr a -> Int -> IO ()
primitive hGetBuf	    	 :: Handle -> Ptr a -> Int -> IO Int

primitive hIsTerminalDevice	 :: Handle -> IO Bool
primitive hGetEcho		 :: Handle -> IO Bool
primitive hSetEcho		 :: Handle -> Bool -> IO ()

-----------------------------------------------------------------------------
-- Non-standard extensions 
-- (likely to disappear when IO library is more complete)
--
-- keep them around for now.

-- C library style test for EOF (doesn't obey Haskell semantics)
primitive hugsHIsEOF "hugsHIsEOF" :: Handle -> IO Bool
hugsIsEOF             :: IO Bool
hugsIsEOF              = hugsHIsEOF stdin
