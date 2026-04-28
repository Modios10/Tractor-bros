library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity debounce is
    Generic (
        WAIT_CYCLES : integer := 1_000_000 -- 20ms a 50MHz
    );
    Port (
        clk        : in  std_logic;
        sig_in     : in  std_logic;
        sig_stable : out std_logic
    );
end debounce;

architecture rtl of debounce is
    signal timer : integer range 0 to WAIT_CYCLES := 0;
    signal state : std_logic := '1';
begin
    sig_stable <= state;

    process(clk)
    begin
        if rising_edge(clk) then
            if sig_in = state then
                timer <= 0;
            elsif timer < WAIT_CYCLES then
                timer <= timer + 1;
            else
                state <= sig_in;
                timer <= 0;
            end if;
        end if;
    end process;
end rtl;
